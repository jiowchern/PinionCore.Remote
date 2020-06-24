﻿using System;
using System.Net;
using System.Net.Sockets;
using Regulus.Network;
using Regulus.Network.Rudp;
using Regulus.Utility;

namespace Regulus.Remote.Ghost
{
	public partial class Agent
	{
		private class ConnectStage : IStatus
		{
			public event Action<IPeer> DoneEvent;
			public event Action FailEvent;


			private readonly IProvider _ConnectProvider;
			private readonly IConnectable _Peer;
			
			private readonly ConnectGhost _Connect;
			System.Action _DoReturn;

			public ConnectStage(IProvider provider,  IConnectable connecter)
			{                
			    _Peer = connecter;                

				_ConnectProvider = provider;
				_Connect = new ConnectGhost();
				_DoReturn = () => { };
			}

			void IStatus.Enter()
			{
				_Connect.ConnectedEvent += _StartConnect;
				_Bind(_ConnectProvider);

				Singleton<Log>.Instance.WriteInfo("Agent connect status start .");
			}

			private void _Bind(IProvider provider)
			{
				provider.Add(_Connect);
				provider.Ready(_Connect.Id);
			}

			private void _Unbind(IProvider provider)
			{
				provider.Remove(_Connect.Id);
			}

			private void _StartConnect(IPEndPoint ip, Value<bool> result)
			{
				Singleton<Log>.Instance.WriteInfo("connect start...");
				

				try
				{


					_Peer.Connect(ip, (connect_result) => {

						_DoReturn = () => {
							result.SetValue(connect_result);
						};
						
						if(connect_result)
						{
							Singleton<Log>.Instance.WriteInfo("agent connect success.");
							DoneEvent(_Peer);
						}
						else
						{
							FailEvent();
						}
						
					} );
					
				}
				catch (Exception e)
				{
					Singleton<Log>.Instance.WriteInfo(string.Format("begin connect fail {0}.", e));
					
				}
				
			}

			void IStatus.Leave()
			{
				_Unbind(_ConnectProvider);

				
				Singleton<Log>.Instance.WriteInfo("Agent connect status leave.");
				_DoReturn();
			}

			void IStatus.Update()
			{
			}

			

			
		}
	}
}
