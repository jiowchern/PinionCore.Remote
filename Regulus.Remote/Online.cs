﻿using System;

namespace Regulus.Remote
{
	public interface IOnline
	{
		double Ping { get; }

		void Disconnect();
	}

	public class OnlineGhost : IOnline, IGhost
	{
		private readonly AgentCore _Agent;

		public Guid Id { get; private set; }

		public OnlineGhost()
		{
			Id = Guid.NewGuid();
		}

		public OnlineGhost(AgentCore agent) : this()
		{
			_Agent = agent;
		}

		

		Guid IGhost.GetID()
		{
			return Id;
		}

	    object IGhost.GetInstance()
	    {
	        return this;
	    }

	    

		bool IGhost.IsReturnType()
		{
			return false;
		}

	    private event CallMethodCallback _CallMethodEvent;

	    event CallMethodCallback IGhost.CallMethodEvent
	    {
	        add { this._CallMethodEvent += value; }
	        remove { this._CallMethodEvent -= value; }
	    }

	    double IOnline.Ping
		{
			get { return TimeSpan.FromTicks(_Agent.Ping).TotalSeconds; }
		}
		public event System.Action DisconnectEvent;
		void IOnline.Disconnect()
		{
			DisconnectEvent();
		}
	}
}
