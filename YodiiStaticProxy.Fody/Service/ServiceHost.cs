#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\ServiceHost.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CK.Core;
using Yodii.Model;
using YodiiStaticProxy.Fody.Log;

namespace YodiiStaticProxy.Fody.Service
{
    internal class ServiceHost : IServiceHost, ILogCenter
    {
        readonly CatchExceptionGeneration _catchMode;

        readonly IList<ILogErrorCaught> _untrackedErrors;
        readonly bool UseStaticProxyGeneration;
        readonly List<IServiceHostConfiguration> _configurations;
        int _currentDepth;
        int _nextLSN;
        readonly Dictionary<Type, ServiceProxyBase> _proxies;
        Assembly _proxyAssembly;

        /// <summary>
        ///     This is set to a non null function when calls to services are not allowed:
        ///     when loading a plugin (from its constructor) or when executing PreStart and Stop method.
        ///     The input type is the called interface type.
        /// </summary>
        internal Func<Type, ServiceCallBlockedException> CallServiceBlocker;

        public ServiceHost(CatchExceptionGeneration catchMode, bool useStaticProxyGeneration)
        {
            EventSender = this;
            _catchMode = catchMode;
            UseStaticProxyGeneration = useStaticProxyGeneration;
            _proxies = new Dictionary<Type, ServiceProxyBase>();
            _currentDepth = 0;
            DefaultConfiguration = new SimpleServiceHostConfiguration();
            _configurations = new List<IServiceHostConfiguration> {DefaultConfiguration};

            _untrackedErrors = new List<ILogErrorCaught>();
            UntrackedErrors = new CKReadOnlyListOnIList<ILogErrorCaught>(_untrackedErrors);
        }

        internal object EventSender { get; set; }

        public event EventHandler<LogEventArgs> EventCreating;
        public event EventHandler<LogEventArgs> EventCreated;
        public IReadOnlyList<ILogErrorCaught> UntrackedErrors { get; }

        public ISimpleServiceHostConfiguration DefaultConfiguration { get; }

        public void Add(IServiceHostConfiguration configurator)
        {
            if(configurator == null) throw new ArgumentNullException();
            if(!_configurations.Contains(configurator))
            {
                _configurations.Add(configurator);
            }
        }

        public void Remove(IServiceHostConfiguration configurator)
        {
            if(configurator == null) throw new ArgumentNullException();
            if(configurator != DefaultConfiguration)
            {
                _configurations.Remove(configurator);
            }
        }

        public void ApplyConfiguration()
        {
            foreach (var proxy in _proxies.Values)
            {
                ApplyConfiguration(proxy);
            }
        }

        internal void HardStop()
        {
            foreach (var s in _proxies.Values)
            {
                if(!s.IsExternalService) s.SetPluginImplementation(null);
            }
        }

        internal IDisposable BlockServiceCall(Func<Type, ServiceCallBlockedException> f)
        {
            Debug.Assert(CallServiceBlocker == null);
            CallServiceBlocker = f;
            return Util.CreateDisposableAction(() => CallServiceBlocker = null);
        }

        internal ServiceProxyBase EnsureProxyForDynamicService(IServiceInfo service)
        {
            Debug.Assert(service != null);
            var serviceType = Assembly.Load(service.AssemblyInfo.AssemblyName).GetType(service.ServiceFullName);
            Debug.Assert(typeof (IYodiiService).IsAssignableFrom(serviceType) && serviceType != typeof (IYodiiService));
            return EnsureProxy(serviceType, false);
        }

        internal ServiceProxyBase EnsureProxyForDynamicService(Type serviceType)
        {
            Debug.Assert(typeof (IYodiiService).IsAssignableFrom(serviceType) && serviceType != typeof (IYodiiService));
            return EnsureProxy(serviceType, false);
        }

        internal ServiceProxyBase EnsureProxyForExternalService(Type interfaceType, object externalImplementation)
        {
            Debug.Assert(externalImplementation != null);
            var proxy = EnsureProxy(interfaceType, true);
            proxy.SetExternalImplementation(externalImplementation);
            return proxy;
        }

        ServiceProxyBase EnsureProxy(Type interfaceType, bool isExternalService)
        {
            ServiceProxyBase proxy;
            if(!_proxies.TryGetValue(interfaceType, out proxy))
            {
                if(UseStaticProxyGeneration)
                {
                    if(_proxyAssembly == null)
                    {
                        var assemblyLocation =
                            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\lib"));
                        _proxyAssembly = Assembly.LoadFrom(assemblyLocation);
                    }

                    Debug.Write(_proxyAssembly.GetTypes().ToString());
                }
                else
                {
                    var definition = new DefaultProxyDefinition(interfaceType, _catchMode);
                    //      proxy = ProxyFactory.CreateProxy(definition);
                    proxy.Initialize(this, isExternalService);
                }

                _proxies.Add(interfaceType, proxy);
                if(!isExternalService)
                    _proxies.Add(typeof (IService<>).MakeGenericType(interfaceType), proxy);
                ApplyConfiguration(proxy);
            }
            return proxy;
        }

        /// <summary>
        ///     For tests only.
        /// </summary>
        internal ServiceProxyBase SetManualProxy(Type interfaceType, ServiceProxyBase proxy)
        {
            ServiceProxyBase current;
            if(_proxies.TryGetValue(interfaceType, out current))
            {
                _proxies[interfaceType] = proxy;
                proxy.SetPluginImplementation(current.Implementation);
            }
            else
            {
                _proxies.Add(interfaceType, proxy);
            }
            proxy.Initialize(this, false);
            ApplyConfiguration(proxy);
            return current;
        }

        void ApplyConfiguration(ServiceProxyBase proxy)
        {
            for (var i = 0; i < proxy.MethodEntries.Length; ++i)
            {
                var o = ServiceLogMethodOptions.None;
                foreach (var cfg in _configurations)
                {
                    o |= cfg.GetOptions(proxy.MethodEntries[i].Method);
                }
                proxy.MethodEntries[i].LogOptions = o;
            }
            for (var i = 0; i < proxy.EventEntries.Length; ++i)
            {
                var o = ServiceLogEventOptions.None;
                foreach (var cfg in _configurations)
                {
                    o |= cfg.GetOptions(proxy.EventEntries[i].Event);
                }
                proxy.EventEntries[i].LogOptions = o;
            }
        }

        #region Interception Log methods.

        /// <summary>
        ///     Called when a method is entered.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="logOptions"></param>
        /// <returns></returns>
        internal LogMethodEntry LogMethodEnter(MethodInfo m, ServiceLogMethodOptions logOptions)
        {
            Debug.Assert(logOptions != 0);
            var me = new LogMethodEntry();
            if((logOptions & ServiceLogMethodOptions.Leave) == 0)
            {
                me.InitClose(++_nextLSN, _currentDepth, m);
                // Emits the "Created" event.
                var h = EventCreated;
                if(h != null) h(EventSender, me);
            }
            else
            {
                me.InitOpen(++_nextLSN, _currentDepth++, m);
                // Emits the "Creating" event.
                var h = EventCreating;
                if(h != null) h(EventSender, me);
            }
            return me;
        }

        /// <summary>
        ///     Called whenever an exception occured in a logged method.
        ///     The existing entry may be closed or opened. If it is opened, we first
        ///     send the EventCreated event for the error entry before sending
        ///     the EventCreated event for the method itself.
        ///     We privilegiate here a hierarchical view: the error will be received before the end of the method.
        /// </summary>
        /// <param name="me">Existing entry.</param>
        /// <param name="ex">Exception raised.</param>
        internal void LogMethodError(LogMethodEntry me, Exception ex)
        {
            var l = new LogMethodEntryError(++_nextLSN, me, ex);
            var h = EventCreated;
            if(me.SetError(l))
            {
                // Entry was opened.
                --_currentDepth;
                if(h != null)
                {
                    // We first send the "Created" event for the error entry.
                    h(EventSender, l);
                    // We then send the "Created" event for the method entry.
                    h(EventSender, me);
                }
                else _untrackedErrors.Add(l);
            }
            else
            {
                // Entry is already closed: just send the error entry.
                if(h != null) h(EventSender, l);
                else _untrackedErrors.Add(l);
            }
            Debug.Assert(!me.IsCreating, "SetError closed the event, whatever its status was.");
        }

        /// <summary>
        ///     Called whenever an exception occured in a non logged method.
        /// </summary>
        /// <param name="m">The culprit method.</param>
        /// <param name="ex">The exception raised.</param>
        internal void LogMethodError(MethodInfo m, Exception ex)
        {
            var l = new LogMethodError(++_nextLSN, _currentDepth, m, ex);
            // Send the "Created" event for the error entry.
            var h = EventCreated;
            if(h != null) h(EventSender, l);
            else _untrackedErrors.Add(l);
        }

        /// <summary>
        ///     Called when a method with an opened entry succeeds.
        /// </summary>
        /// <param name="me"></param>
        internal void LogMethodSuccess(LogMethodEntry me)
        {
            Debug.Assert(me.IsCreating);
            --_currentDepth;
            me.Close();
            var h = EventCreated;
            if(h != null) h(EventSender, me);
        }

        internal LogEventEntry LogEventEnter(EventInfo e, ServiceLogEventOptions logOptions)
        {
            Debug.Assert(logOptions != 0);
            var ee = new LogEventEntry();
            if((logOptions & ServiceLogEventOptions.EndRaise) == 0)
            {
                ee.InitClose(++_nextLSN, _currentDepth, e);
                // Emits the "Created" event.
                var h = EventCreated;
                if(h != null) h(EventSender, ee);
            }
            else
            //if( (logOptions & ServiceLogEventOptions.StartRaise) != 0) //if we are only logging the EndRaise, we should NOT log through LogEventEnter
            {
                ee.InitOpen(++_nextLSN, _currentDepth++, e);
                // Emits the "Creating" event.
                var h = EventCreating;
                if(h != null) h(EventSender, ee);
            }
            return ee;
        }

        /// <summary>
        ///     Called at the end of an event raising that have an existing opened log entry.
        ///     Entries that have been created by <see cref="LogEventError(EventInfo,MethodInfo,Exception)" /> (because an
        ///     exception
        ///     has been raised by at least one receiver) are not tracked.
        /// </summary>
        /// <param name="ee">The entry of the event that ended.</param>
        internal void LogEventEnd(LogEventEntry ee)
        {
            Debug.Assert(ee.IsCreating);
            --_currentDepth;
            ee.Close();
            var h = EventCreated;
            if(h != null) h(EventSender, ee);
        }

        /// <summary>
        ///     Called whenever the recipient of an event raises an exception and the event log already exists.
        ///     This appends the error to the error list of the event entry.
        /// </summary>
        /// <param name="ee">Existing event log entry.</param>
        /// <param name="target">Culprit method.</param>
        /// <param name="ex">Exception raised by the culprit method.</param>
        internal void LogEventError(LogEventEntry ee, MethodInfo target, Exception ex)
        {
            var l = new LogEventEntryError(++_nextLSN, ee, target, ex);
            ee.AddError(l);
            var h = EventCreated;
            if(h != null) h(EventSender, l);
            else _untrackedErrors.Add(l);
        }

        /// <summary>
        ///     Called whenever the recipient of an event raises an exception and the event is not
        ///     yet logged (no <see cref="LogEventEntry" /> exists). This creates the entry for the event
        ///     and the associated error.
        /// </summary>
        /// <param name="e">The reflected event info.</param>
        /// <param name="target">Culprit method.</param>
        /// <param name="ex">Exception raised by the culprit method.</param>
        /// <returns>The created event entry that holds the error.</returns>
        internal LogEventEntry LogEventError(EventInfo e, MethodInfo target, Exception ex)
        {
            // This LogEventEntry is an hidden one. We do not emit it.
            var ee = new LogEventEntry();
            var l = new LogEventEntryError(++_nextLSN, ee, target, ex);
            ee.InitError(++_nextLSN, _currentDepth, e, l);

            // Emits the error.
            var h = EventCreated;
            if(h != null) h(EventSender, l);
            else _untrackedErrors.Add(l);

            return ee;
        }

        /// <summary>
        ///     Called when an event is raised by a stopped service and both
        ///     <see cref="ServiceLogEventOptions.LogSilentEventRunningStatusError" />
        ///     and <see cref="ServiceLogEventOptions.SilentEventRunningStatusError" /> are set.
        /// </summary>
        internal void LogEventNotRunningError(EventInfo eventInfo, bool serviceIsDisabled)
        {
            var l = new LogEventNotRunningError(++_nextLSN, _currentDepth, eventInfo,
                serviceIsDisabled);
            var h = EventCreated;
            if(h != null) h(EventSender, l);
        }

        public void ExternalLog(string message, object extraData)
        {
            var e = new LogExternalEntry(_nextLSN++, _currentDepth, message ?? string.Empty, extraData);
            var h = EventCreated;
            if(h != null) h(EventSender, e);
        }

        public void ExternalLogError(Exception ex, MemberInfo optionalExplicitCulprit, string message, object extraData)
        {
            if(message == null) message = string.Empty;
            if(ex == null)
            {
                ex = new ArgumentNullException();
//                message = R.ExternalLogErrorMissException + message;
                message = "ExternalLogErrorMissException" + message;
            }
            var e = new LogExternalErrorEntry(_nextLSN++, _currentDepth, ex, optionalExplicitCulprit,
                message, extraData);
            var h = EventCreated;
            if(h != null) h(EventSender, e);
            else _untrackedErrors.Add(e);
        }

        #endregion

        #region IServiceHost Members

        IServiceUntyped IServiceHost.InjectExternalService(Type interfaceType, object currentImplementation)
        {
//            if( currentImplementation == null ) throw new ArgumentNullException( "currentImplementation", R.ExternalImplRequiredAsANonNullObject );
            if(currentImplementation == null)
                throw new ArgumentNullException("currentImplementation", "ExternalImplRequiredAsANonNullObject");
            return EnsureProxyForExternalService(interfaceType, currentImplementation);
        }

        IServiceUntyped IServiceHost.EnsureProxyForDynamicService(Type interfaceType)
        {
            if(!typeof (IYodiiService).IsAssignableFrom(interfaceType) || interfaceType == typeof (IYodiiService))
            {
//                throw new ArgumentException( R.InterfaceMustExtendIYodiiService, "interfaceType" );
                throw new ArgumentException("InterfaceMustExtendIYodiiService", "interfaceType");
            }
            return EnsureProxyForDynamicService(interfaceType);
        }

        IServiceUntyped IServiceHost.GetProxy(Type interfaceType)
        {
            return _proxies.GetValueWithDefault(interfaceType, null);
        }

        IService<T> IServiceHost.EnsureProxyForDynamicService<T>()
        {
            return (IService<T>) EnsureProxyForDynamicService(typeof (T));
        }

        #endregion
    }
}