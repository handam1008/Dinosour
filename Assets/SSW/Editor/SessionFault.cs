using System;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;

namespace SSW
{
    public static class SessionFault
    {
        public static int Count { get; private set; }

        public static int StartCount { get; private set; }

        public static void ArmStart(Unity.Netcode.NetworkManager manager)
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            UnityEditor.EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEditor.EditorApplication.update -= tick;
                    return;
                }
                if (!manager.IsListening || !manager.IsClient || manager.IsServer || manager.IsConnectedClient) return;
                UnityEditor.EditorApplication.update -= tick;
                StartCount++;
                manager.Shutdown();
            };
            UnityEditor.EditorApplication.update += tick;
        }

        public static void Arm(ISession session, bool after, string method = "RemovePlayerAsync")
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            object lobby = session.GetType().GetField("LobbyHandler", flags).GetValue(session);
            FieldInfo field = lobby.GetType().GetField("m_LobbyService", flags);
            object service = field.GetValue(lobby);
            var proxy = new LeaveProxy(field.FieldType, lobby, field, service, after, method);
            field.SetValue(lobby, proxy.GetTransparentProxy());
        }

        sealed class LeaveProxy : RealProxy
        {
            readonly object _lobby;
            readonly FieldInfo _field;
            readonly object _service;
            readonly bool _after;
            readonly string _method;

            public LeaveProxy(Type type, object lobby, FieldInfo field, object service, bool after, string method) : base(type)
            {
                _lobby = lobby;
                _field = field;
                _service = service;
                _after = after;
                _method = method;
            }

            public override IMessage Invoke(IMessage message)
            {
                var call = (IMethodCallMessage)message;
                try
                {
                    object value;
                    if (call.MethodName == _method)
                    {
                        _field.SetValue(_lobby, _service);
                        Count++;
                        Task request = _after ? (Task)call.MethodBase.Invoke(_service, call.Args) : Task.CompletedTask;
                        value = Fail(request);
                    }
                    else value = call.MethodBase.Invoke(_service, call.Args);
                    return new ReturnMessage(value, call.Args, call.ArgCount, call.LogicalCallContext, call);
                }
                catch (TargetInvocationException error)
                {
                    return new ReturnMessage(error.InnerException, call);
                }
            }

            static async Task Fail(Task request)
            {
                await request;
                throw new SessionException("Injected leave response failure", SessionError.Unknown, null);
            }
        }
    }
}
