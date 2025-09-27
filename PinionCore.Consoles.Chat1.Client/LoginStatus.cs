using PinionCore.Remote;
using PinionCore.Consoles.Chat1.Common;
using PinionCore.Utility;
using System;

namespace PinionCore.Consoles.Chat1.Client
{
    internal class LoginStatus : PinionCore.Utility.IStatus
    {
        readonly ILogin _Login;
        private readonly Command _Command;

        public LoginStatus(ILogin login, Command command)
        {
            _Login = login;
            _Command = command;
        }

        void IStatus.Enter()
        {
            _LoginCommand(_Login);
        }

        private void _LoginCommand(ILogin gpi)
        {
            _Command.RemotingRegister<string, bool>("login", (name) => gpi.Login(name), _LoginResult);
        }

        private void _LoginResult(bool login_result)
        {
            System.Console.WriteLine($"Login is {login_result}");
        }

        void IStatus.Leave()
        {
            _Command.Unregister("login");
        }

        void IStatus.Update()
        {
            
        }
    }
}
