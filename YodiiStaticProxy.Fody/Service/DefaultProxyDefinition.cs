#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Service\DefaultProxyDefinition.cs) is part of CiviKey. 
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
using System.Reflection;
using Yodii.Model;

namespace YodiiStaticProxy.Fody.Service
{
    internal class DefaultProxyDefinition : IProxyDefinition
    {
        readonly CatchExceptionGeneration _errorCatch;

        public DefaultProxyDefinition(Type typeInterface,
            CatchExceptionGeneration errorCatch = CatchExceptionGeneration.HonorIgnoreExceptionAttribute)
        {
            if(!typeInterface.IsInterface)
            {
//				throw new ArgumentException( R.TypeMustBeAnInterface, "typeInterface" );
                throw new ArgumentException("TypeMustBeAnInterface", "typeInterface");
            }
            TypeInterface = typeInterface;
            _errorCatch = errorCatch;
            IsDynamicService = typeof (IYodiiService).IsAssignableFrom(typeInterface);
        }

        public Type TypeInterface { get; }

        public bool IsDynamicService { get; }

        public Type ProxyBase
        {
            get { return typeof (ServiceProxyBase); }
        }

        public ProxyOptions GetEventOptions(EventInfo e)
        {
            var opt = new ProxyOptions();

            opt.CatchExceptions = _errorCatch == CatchExceptionGeneration.Always
                                  || (_errorCatch == CatchExceptionGeneration.HonorIgnoreExceptionAttribute
                                      && !e.IsDefined(typeof (IgnoreExceptionAttribute), false));

            if(IsDynamicService)
            {
                var stopAllowed = e.IsDefined(typeof (IgnoreServiceStoppedAttribute), false);
                opt.RuntimeCheckStatus = stopAllowed
                    ? ProxyOptions.CheckStatus.NotDisabled
                    : ProxyOptions.CheckStatus.Running;
            }
            else opt.RuntimeCheckStatus = ProxyOptions.CheckStatus.None;

            return opt;
        }

        public ProxyOptions GetPropertyMethodOptions(PropertyInfo p, MethodInfo m)
        {
            var opt = new ProxyOptions();

            opt.CatchExceptions = _errorCatch == CatchExceptionGeneration.Always
                                  || (_errorCatch == CatchExceptionGeneration.HonorIgnoreExceptionAttribute
                                      &&
                                      !(p.IsDefined(typeof (IgnoreExceptionAttribute), false) ||
                                        m.IsDefined(typeof (IgnoreExceptionAttribute), false)));

            if(IsDynamicService)
            {
                var stopAllowed = p.IsDefined(typeof (IgnoreServiceStoppedAttribute), false) ||
                                  m.IsDefined(typeof (IgnoreServiceStoppedAttribute), false);
                opt.RuntimeCheckStatus = stopAllowed
                    ? ProxyOptions.CheckStatus.NotDisabled
                    : ProxyOptions.CheckStatus.Running;
            }
            else opt.RuntimeCheckStatus = ProxyOptions.CheckStatus.None;
            return opt;
        }

        public ProxyOptions GetMethodOptions(MethodInfo m)
        {
            var opt = new ProxyOptions();

            opt.CatchExceptions = _errorCatch == CatchExceptionGeneration.Always
                                  || (_errorCatch == CatchExceptionGeneration.HonorIgnoreExceptionAttribute
                                      && !m.IsDefined(typeof (IgnoreExceptionAttribute), false));

            if(IsDynamicService)
            {
                var stopAllowed = m.IsDefined(typeof (IgnoreServiceStoppedAttribute), false);
                opt.RuntimeCheckStatus = stopAllowed
                    ? ProxyOptions.CheckStatus.NotDisabled
                    : ProxyOptions.CheckStatus.Running;
            }
            else opt.RuntimeCheckStatus = ProxyOptions.CheckStatus.None;
            return opt;
        }
    }
}