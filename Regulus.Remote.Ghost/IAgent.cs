using Regulus.Network;
using System;

namespace Regulus.Remote.Ghost
{
    public interface IAgent : INotifierQueryable , IDisposable
    {        
        /// <summary>
        ///     Active
        /// </summary>
        bool Active { get; }
        /// <summary>
        ///     Ping
        /// </summary>
        long Ping { get; }


        /// <summary>
        /// ���~����k�I�s
        /// �p�G�I�s����k�ѼƦ��~�h�|�^�Ǧ��T��.
        /// �ƥ�Ѽ�:
        ///     1.��k�W��
        ///     2.���~�T��
        /// �|�o�ͦ��T���q�`�O�]��client�Pserver�������ۮe�ҭP.
        /// </summary>
        event Action<string, string> ErrorMethodEvent;


       


        
        void Update();
    }
}