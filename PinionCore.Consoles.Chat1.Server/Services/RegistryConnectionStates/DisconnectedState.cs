using System;
using PinionCore.Utility;

namespace PinionCore.Consoles.Chat1.Server.Services.RegistryConnectionStates
{
    /// <summary>
    /// 未連接狀態
    /// </summary>
    internal class DisconnectedState : IStatus
    {
        private readonly PinionCore.Utility.Log _log;
        private bool _connectTriggered = false;

        public event Action OnStartConnect;

        public DisconnectedState(PinionCore.Utility.Log log)
        {
            _log = log;
        }

        void IStatus.Enter()
        {
            _log.WriteInfo("Registry 狀態: 未連接");
        }

        void IStatus.Update()
        {
            // 只觸發一次連接嘗試
            if (!_connectTriggered)
            {
                _connectTriggered = true;
                OnStartConnect?.Invoke();
            }
        }

        void IStatus.Leave()
        {
        }
    }
}
