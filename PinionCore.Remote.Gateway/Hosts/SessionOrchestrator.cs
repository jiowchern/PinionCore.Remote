using System;
using System.Collections.Generic;
using System.Linq;
using PinionCore.Remote.Gateway.Protocols;

namespace PinionCore.Remote.Gateway.Hosts
{
    interface IRouterSessionMembership
    {
        void Join(ISession session);
        void Leave(ISession session);
    }

    interface IRouterServiceRegistry
    {
        void Register(uint group, IGameService service);
        void Unregister(IGameService service);
    }
    internal class Router : IRouterSessionMembership , IRouterServiceRegistry
    {
        private sealed class Assignment
        {
            private readonly System.Collections.Generic.List<System.Action<uint>> _userIdCallbacks;
            internal ISession Session { get; }
            internal Registration Registration { get; }
            internal uint Group { get; }
            internal uint UserId { get; private set; }
            internal IServiceSession ServiceSession { get; set; }
            internal bool Bound { get; set; }
            internal bool Releasing { get; set; }
            internal bool HasUserId { get; private set; }

            internal Assignment(ISession session, Registration registration, uint group)
            {
                Session = session;
                Registration = registration;
                Group = group;
                _userIdCallbacks = new System.Collections.Generic.List<System.Action<uint>>();
            }

            internal void OnUserIdAssigned(System.Action<uint> callback)
            {
                if (HasUserId)
                {
                    callback(UserId);
                }
                else
                {
                    _userIdCallbacks.Add(callback);
                }
            }

            internal void SetUserId(uint userId)
            {
                if (HasUserId)
                {
                    return;
                }

                HasUserId = true;
                UserId = userId;

                foreach (var callback in _userIdCallbacks)
                {
                    callback(userId);
                }

                _userIdCallbacks.Clear();
            }
        }

        private sealed class Registration
        {
            internal IGameService Service { get; }
            internal uint Group { get; }
            internal Dictionary<ISession, Assignment> SessionAssignments { get; }
            internal Dictionary<uint, Assignment> AssignmentsByUserId { get; }
            internal Action<IServiceSession> SupplyHandler { get; }
            internal Action<IServiceSession> UnsupplyHandler { get; }
            internal Dictionary<uint, IServiceSession> PendingSessions { get; }

            internal Registration(IGameService service, uint group, Action<IServiceSession> supplyHandler, Action<IServiceSession> unsupplyHandler)
            {
                Service = service;
                Group = group;
                SupplyHandler = supplyHandler;
                UnsupplyHandler = unsupplyHandler;
                SessionAssignments = new Dictionary<ISession, Assignment>();
                AssignmentsByUserId = new Dictionary<uint, Assignment>();
                PendingSessions = new Dictionary<uint, IServiceSession>();
            }

            internal void Subscribe()
            {
                Service.UserNotifier.Base.Supply += SupplyHandler;
                Service.UserNotifier.Base.Unsupply += UnsupplyHandler;
            }

            internal void Unsubscribe()
            {
                Service.UserNotifier.Base.Supply -= SupplyHandler;
                Service.UserNotifier.Base.Unsupply -= UnsupplyHandler;
            }
        }

        private readonly object _syncRoot;
        private readonly HashSet<ISession> _sessions;
        private readonly Dictionary<ISession, Dictionary<uint, Assignment>> _sessionAssignments;
        private readonly Dictionary<uint, List<Registration>> _registrationsByGroup;
        private readonly Dictionary<IGameService, Registration> _registrationsByService;

        public Router()
        {
            _syncRoot = new object();
            _sessions = new HashSet<ISession>();
            _sessionAssignments = new Dictionary<ISession, Dictionary<uint, Assignment>>();
            _registrationsByGroup = new Dictionary<uint, List<Registration>>();
            _registrationsByService = new Dictionary<IGameService, Registration>();
        }

        public void Join(ISession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (_syncRoot)
            {
                if (!_sessions.Add(session))
                {
                    return;
                }

                if (!_sessionAssignments.ContainsKey(session))
                {
                    _sessionAssignments[session] = new Dictionary<uint, Assignment>();
                }

                foreach (var group in _registrationsByGroup.Keys.ToArray())
                {
                    _EnsureSessionAssignment(session, group);
                }
            }
        }

        public void Leave(ISession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            lock (_syncRoot)
            {
                if (!_sessions.Remove(session))
                {
                    return;
                }

                if (!_sessionAssignments.TryGetValue(session, out var groupAssignments))
                {
                    return;
                }

                foreach (var assignment in groupAssignments.Values.ToList())
                {
                    _ReleaseAssignment(assignment, reassign: false);
                }

                _sessionAssignments.Remove(session);
            }
        }

        public void Register(uint group, IGameService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_syncRoot)
            {
                if (_registrationsByService.ContainsKey(service))
                {
                    return;
                }

                var registration = new Registration(
                    service,
                    group,
                    svc => _OnServiceSessionSupplied(service, svc),
                    svc => _OnServiceSessionUnsupplied(service, svc));

                _registrationsByService.Add(service, registration);

                if (!_registrationsByGroup.TryGetValue(group, out var registrations))
                {
                    registrations = new List<Registration>();
                    _registrationsByGroup[group] = registrations;
                }

                registrations.Add(registration);
                registration.Subscribe();

                foreach (var session in _sessions)
                {
                    _EnsureSessionAssignment(session, group);
                }
            }
        }

        public void Unregister(IGameService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (_syncRoot)
            {
                if (!_registrationsByService.TryGetValue(service, out var registration))
                {
                    return;
                }

                registration.Unsubscribe();
                _registrationsByService.Remove(service);

                if (_registrationsByGroup.TryGetValue(registration.Group, out var registrations))
                {
                    registrations.Remove(registration);
                    if (registrations.Count == 0)
                    {
                        _registrationsByGroup.Remove(registration.Group);
                    }
                }

                foreach (var assignment in registration.SessionAssignments.Values.ToList())
                {
                    _ReleaseAssignment(assignment, reassign: true);
                }
            }
        }

        private void _EnsureSessionAssignment(ISession session, uint group)
        {
            if (!_registrationsByGroup.TryGetValue(group, out var registrations) || registrations.Count == 0)
            {
                return;
            }

            if (!_sessionAssignments.TryGetValue(session, out var groupAssignments))
            {
                groupAssignments = new Dictionary<uint, Assignment>();
                _sessionAssignments[session] = groupAssignments;
            }

            if (groupAssignments.TryGetValue(group, out var current) && current != null && current.Bound)
            {
                return;
            }

            foreach (var registration in registrations)
            {
                if (_TryAttach(registration, session, group, out var assignment))
                {
                    groupAssignments[group] = assignment;
                    return;
                }
            }

            groupAssignments.Remove(group);
        }

        private bool _TryAttach(Registration registration, ISession session, uint group, out Assignment assignment)
        {
            if (registration.SessionAssignments.TryGetValue(session, out assignment))
            {
                return true;
            }

            var newAssignment = new Assignment(session, registration, group);
            registration.SessionAssignments[session] = newAssignment;

            if (!_sessionAssignments.TryGetValue(session, out var groupAssignments))
            {
                groupAssignments = new Dictionary<uint, Assignment>();
                _sessionAssignments[session] = groupAssignments;
            }

            groupAssignments[group] = newAssignment;

            newAssignment.OnUserIdAssigned(userId =>
            {
                if (!registration.SessionAssignments.TryGetValue(session, out var current) || !ReferenceEquals(current, newAssignment))
                {
                    registration.PendingSessions.Remove(userId);
                    return;
                }

                registration.AssignmentsByUserId[userId] = newAssignment;

                if (registration.PendingSessions.TryGetValue(userId, out var pendingSession))
                {
                    registration.PendingSessions.Remove(userId);
                    _BindAssignment(newAssignment, pendingSession);
                }
                else
                {
                    _TryBindImmediate(newAssignment);
                }
            });

            var joinValue = registration.Service.Join();

            void OnJoined(uint userId)
            {
                lock (_syncRoot)
                {
                    joinValue.OnValue -= OnJoined;

                    if (!registration.SessionAssignments.TryGetValue(session, out var current) || !ReferenceEquals(current, newAssignment))
                    {
                        newAssignment.SetUserId(userId);
                        return;
                    }

                    newAssignment.SetUserId(userId);
                }
            }

            joinValue.OnValue += OnJoined;

            assignment = newAssignment;
            return true;
        }

        private void _TryBindImmediate(Assignment assignment)
        {
            foreach (var candidate in assignment.Registration.Service.UserNotifier.Collection)
            {
                if (candidate.Id.Value == assignment.UserId)
                {
                    _BindAssignment(assignment, candidate);
                    break;
                }
            }
        }

        private void _BindAssignment(Assignment assignment, IServiceSession serviceSession)
        {
            assignment.ServiceSession = serviceSession;
            var success = assignment.Session.Set(assignment.Group, serviceSession);
            if (!success)
            {
                assignment.ServiceSession = null;
                assignment.Registration.Service.Leave(assignment.UserId).GetAwaiter().GetResult();
                assignment.Registration.AssignmentsByUserId.Remove(assignment.UserId);
                assignment.Registration.SessionAssignments.Remove(assignment.Session);

                if (_sessionAssignments.TryGetValue(assignment.Session, out var groupAssignments))
                {
                    groupAssignments.Remove(assignment.Group);
                }

                _EnsureSessionAssignment(assignment.Session, assignment.Group);
                return;
            }

            assignment.Bound = true;
        }

        private void _ReleaseAssignment(Assignment assignment, bool reassign)
        {
            assignment.Registration.SessionAssignments.Remove(assignment.Session);

            if (_sessionAssignments.TryGetValue(assignment.Session, out var groupAssignments) && groupAssignments.TryGetValue(assignment.Group, out var current) && ReferenceEquals(current, assignment))
            {
                groupAssignments.Remove(assignment.Group);
            }

            if (assignment.Bound)
            {
                assignment.Releasing = true;
                assignment.Session.Unset(assignment.Group);
            }

            assignment.OnUserIdAssigned(userId =>
            {
                assignment.Registration.AssignmentsByUserId.Remove(userId);
                assignment.Registration.PendingSessions.Remove(userId);
                assignment.Registration.Service.Leave(userId).GetAwaiter().GetResult();
            });

            if (reassign && _sessions.Contains(assignment.Session))
            {
                _EnsureSessionAssignment(assignment.Session, assignment.Group);
            }
        }

        private void _OnServiceSessionSupplied(IGameService service, IServiceSession serviceSession)
        {
            lock (_syncRoot)
            {
                if (!_registrationsByService.TryGetValue(service, out var registration))
                {
                    return;
                }

                if (!registration.AssignmentsByUserId.TryGetValue(serviceSession.Id.Value, out var assignment))
                {
                    registration.PendingSessions[serviceSession.Id.Value] = serviceSession;
                    return;
                }

                if (assignment.Bound)
                {
                    assignment.ServiceSession = serviceSession;
                    return;
                }

                _BindAssignment(assignment, serviceSession);
            }
        }

        private void _OnServiceSessionUnsupplied(IGameService service, IServiceSession serviceSession)
        {
            lock (_syncRoot)
            {
                if (!_registrationsByService.TryGetValue(service, out var registration))
                {
                    return;
                }

                if (!registration.AssignmentsByUserId.TryGetValue(serviceSession.Id.Value, out var assignment))
                {
                    registration.PendingSessions.Remove(serviceSession.Id.Value);
                    return;
                }

                registration.AssignmentsByUserId.Remove(serviceSession.Id.Value);
                registration.SessionAssignments.Remove(assignment.Session);

                if (_sessionAssignments.TryGetValue(assignment.Session, out var groupAssignments) && groupAssignments.TryGetValue(registration.Group, out var current) && ReferenceEquals(current, assignment))
                {
                    groupAssignments.Remove(registration.Group);
                }

                if (assignment.Bound && !assignment.Releasing)
                {
                    assignment.Session.Unset(registration.Group);
                }

                if (!assignment.Releasing && _sessions.Contains(assignment.Session))
                {
                    _EnsureSessionAssignment(assignment.Session, registration.Group);
                }
            }
        }
    }
}
