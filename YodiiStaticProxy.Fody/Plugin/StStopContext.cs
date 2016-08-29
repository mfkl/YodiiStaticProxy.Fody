﻿#region LGPL License

/*----------------------------------------------------------------------------
* This file (Yodii.Host\Plugin\StStopContext.cs) is part of CiviKey. 
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
using Yodii.Model;

namespace YodiiStaticProxy.Fody.Plugin
{
    internal class StStopContext : StContext, IPreStopContext, IStopContext
    {
        internal readonly bool IsDisabledOnly;

        public StStopContext(PluginProxy plugin, RunningStatus status, Dictionary<object, object> shared,
            bool disableOnly, bool engineStopping)
            : base(plugin, status, shared)
        {
            IsDisabledOnly = disableOnly;
            IsCancellable = !engineStopping;
        }

        internal bool MustDisable
        {
            get { return RunningStatus == RunningStatus.Disabled; }
        }

        public bool IsCancellable { get; }

        public Action<IStartContext> RollbackAction { get; set; }

        public override void Cancel(string message = null, Exception ex = null)
        {
//            if( !IsCancellable ) throw new InvalidOperationException( R.CannotCancelSinceEngineIsStopping );
            if(!IsCancellable) throw new InvalidOperationException("CannotCancelSinceEngineIsStopping");
            base.Cancel(message, ex);
        }

        bool IStopContext.CancellingPreStart
        {
            get { return false; }
        }

        bool IStopContext.HotSwapping
        {
            get { return Status == StStatus.StoppingHotSwap; }
        }

        public override string ToString()
        {
            return string.Format("Stop: {0}, MustDisable={1}, IsDisabledOnly={2}", Plugin.PluginInfo.PluginFullName,
                MustDisable, IsDisabledOnly);
        }
    }
}