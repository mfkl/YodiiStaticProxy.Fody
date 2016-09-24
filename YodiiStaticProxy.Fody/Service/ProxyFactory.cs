#region LGPL License

/*----------------------------------------------------------------------------

* This file (Yodii.Host\Service\ProxyFactory.cs) is part of CiviKey. 

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
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using CK.Reflection;
using Yodii.Model;
using Util = YodiiStaticProxy.Fody.Finders.Util;

namespace YodiiStaticProxy.Fody.Service
{
    internal class ProxyFactory
    {
        static int _typeID;
        static readonly MethodInfo _delegateCombine;
        static readonly MethodInfo _delegateGetInvocationList;
        static readonly MethodInfo _delegateGetMethod;
        static readonly MethodInfo _delegateRemove;
        static readonly MethodInfo _untypedServiceGetMethod = typeof (IServiceUntyped).GetProperty("Service").GetGetMethod();
        static AssemblyBuilder _assemblyBuilder;
        static ModuleBuilder _moduleBuilder;
        static AssemblyName _assemblyName;

        static ProxyFactory()
        {


            //            StrongNameKeyPair kp;

            //            using( Stream stream = Assembly.GetAssembly( typeof( ProxyFactory ) ).GetManifestResourceStream( "Yodii.Host.DynamicKeyPair.DynamicKeyPair.snk" ) )

            //            {

            //                // This is the public key of DynamicKeyPair.snk.

            //                //

            //                // PublicKey : 00240000048000009400000006020000002400005253413100040000010001009fbf2868f04bdf33df4c8c0517bb4c3d743b5b27fcd94009d42d6607446c1887a837e66545221788ecfff8786e85564c839ff56267fe1a3225cd9d8d9caa5aae3ba5d8f67f86ff9dbc5d66f16ba95bacde6d0e02f452fae20022edaea26d31e52870358d0dda69e592ea5cef609a054dac4dbbaa02edc32fb7652df9c0e8e9cd

            //                //

            //                // ServiceProxyBase (defined in CK.Plugin.Host) is internal. 

            //                // The CK.Plugin.Host assembly has an [assembly: InternalsVisibleTo( "CKProxyAssembly, PublicKey=..." )] attribute that allows 

            //                // the dynamic CKProxyAssembly to make use of the ServiceProxyBase as the base class for all the proxies it generates.

            //                //

            //                byte[] result = new byte[stream.Length]; 

            //                stream.Read(result,0,(int)stream.Length);

            //                kp = new StrongNameKeyPair( result );

            //            }

            //            assemblyName.KeyPair = kp;

       
            _delegateGetInvocationList = typeof (Delegate).GetMethod("GetInvocationList", Type.EmptyTypes);
            _delegateGetMethod = typeof (Delegate).GetMethod("get_Method", Type.EmptyTypes);
            Type[] paramTwoDelegates = {typeof (Delegate), typeof (Delegate)};
            _delegateCombine = typeof (Delegate).GetMethod("Combine", paramTwoDelegates);
            _delegateRemove = typeof (Delegate).GetMethod("Remove", paramTwoDelegates);
        }
        
        static void CreateUnavailableImplementation(Type interfaceType, string dynamicTypeName)
        {
            // Defines a public sealed class that only implements typeInterface 

            // and for which all methods throw ServiceNotAvailableException.

            var typeBuilderNotAvailable = _moduleBuilder.DefineType(
                dynamicTypeName,
                TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Serializable,
                typeof (object),
                new[] {interfaceType});


            var mGetTypeFromHandle = typeof (Type).GetMethod("GetTypeFromHandle");

            var cNotAvailableException =
                typeof (ServiceNotAvailableException).GetConstructor(new[] {typeof (Type)});

            foreach (var m in ReflectionHelper.GetFlattenMethods(interfaceType))

            {
                Type[] parameters;

                var mB = ProxyGenerator.CreateInterfaceMethodBuilder(typeBuilderNotAvailable, m,
                    out parameters);

                var g = mB.GetILGenerator();

                g.Emit(OpCodes.Ldtoken, interfaceType);

                g.EmitCall(OpCodes.Call, mGetTypeFromHandle, null);

                g.Emit(OpCodes.Newobj, cNotAvailableException);

                g.Emit(OpCodes.Throw);
            }

            typeBuilderNotAvailable.CreateType();
        }


        /// <summary>
        ///     Creates a proxyfied interface according to the given definition.
        /// </summary>
        /// <param name="definition">Definition of the proxy to build.</param>
        /// <returns></returns>
        public static void CreateStaticProxy(DefaultProxyDefinition definition)

        {
            Debug.Assert(definition.TypeInterface.IsInterface,
                "This check MUST be done by ProxyDefinition implementation.");


            string dynamicTypeName = $"{definition.TypeInterface.Name}_Proxy_{Interlocked.Increment(ref _typeID)}";


            // Defines a public sealed class that implements typeInterface only.
            var typeBuilder = _moduleBuilder.DefineType(
                dynamicTypeName,
                TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Serializable,
                definition.ProxyBase,
                new[] {definition.TypeInterface});


            // This defines the IService<typeInterface> interface.

            if(definition.IsDynamicService)

            {
                typeBuilder.AddInterfaceImplementation(typeof (IServiceUntyped));


                // The proxy object implements both typeInterface and IService<typeInterface> interfaces.

                var serviceInterfaceType = typeof (IService<>).MakeGenericType(definition.TypeInterface);

                typeBuilder.AddInterfaceImplementation(serviceInterfaceType);


                serviceInterfaceType = typeof (IOptionalService<>).MakeGenericType(definition.TypeInterface);

                typeBuilder.AddInterfaceImplementation(serviceInterfaceType);


                serviceInterfaceType = typeof (IOptionalRecommendedService<>).MakeGenericType(definition.TypeInterface);

                typeBuilder.AddInterfaceImplementation(serviceInterfaceType);


                serviceInterfaceType = typeof (IRunnableService<>).MakeGenericType(definition.TypeInterface);

                typeBuilder.AddInterfaceImplementation(serviceInterfaceType);


                serviceInterfaceType = typeof (IRunnableRecommendedService<>).MakeGenericType(definition.TypeInterface);

                typeBuilder.AddInterfaceImplementation(serviceInterfaceType);


                serviceInterfaceType = typeof (IRunningService<>).MakeGenericType(definition.TypeInterface);

                typeBuilder.AddInterfaceImplementation(serviceInterfaceType);
            }

            var pg = new ProxyGenerator(typeBuilder, definition);


            pg.DefineConstructor();


            pg.DefineServiceProperty();


            pg.DefineImplementationProperty();


            pg.ImplementInterface();


            CreateUnavailableImplementation(definition.TypeInterface, dynamicTypeName + "_UN");


            pg.FinalizeStatic();
        }

        class ProxyGenerator
        {
            readonly IProxyDefinition _definition;

            readonly List<EventInfo> _eRefs;

            readonly FieldBuilder _implField;

            readonly List<MethodInfo> _mRefs;

            readonly HashSet<MethodInfo> _processedMethods;
            readonly TypeBuilder _typeBuilder;


            public ProxyGenerator(TypeBuilder typeBuilder, IProxyDefinition definition)
            {
                _typeBuilder = typeBuilder;

                _definition = definition;

                // Define a member variable to hold the implementation

                _implField = typeBuilder.DefineField("_impl", definition.TypeInterface, FieldAttributes.Private);

                _processedMethods = new HashSet<MethodInfo>();

                _mRefs = new List<MethodInfo>();

                _eRefs = new List<EventInfo>();
            }

            public void DefineConstructor()
            {
                // Defines constructor that accepts the typeInterface (the implementation). 
                var ctor = _typeBuilder.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard,
                    new[]
                    {_definition.TypeInterface, typeof (Type), typeof (IList<MethodInfo>), typeof (IList<EventInfo>)});

                // Generates ctor body. 
                {
                    var ctorProxyBase =
                        _definition.ProxyBase.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                            new[]
                            {typeof (object), typeof (Type), typeof (IList<MethodInfo>), typeof (IList<EventInfo>)},
                            null);

                    var g = ctor.GetILGenerator();

                    // Calls base ctor.
                    g.LdArg(0);

                    g.LdArg(1);

                    g.LdArg(2);

                    g.LdArg(3);

                    g.LdArg(4);

                    g.Emit(OpCodes.Call, ctorProxyBase);

                    // _impl = unavailableService;

                    g.LdArg(0);

                    g.LdArg(1);

                    g.Emit(OpCodes.Stfld, _implField);

                    g.Emit(OpCodes.Ret);
                }
            }


            public void DefineServiceProperty()

            {
                // The IService<T>.Service property must return the proxy itself, not _impl typed as T.

                var servicePropertyGet = _typeBuilder.DefineMethod(
                    "get_Service",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    _definition.TypeInterface,
                    Type.EmptyTypes);

                {
                    var g = servicePropertyGet.GetILGenerator();

                    g.Emit(OpCodes.Ldarg_0);

                    g.Emit(OpCodes.Ret);
                }

                var serviceProperty = _typeBuilder.DefineProperty("Service", PropertyAttributes.HasDefault,
                    _definition.TypeInterface, Type.EmptyTypes);

                serviceProperty.SetGetMethod(servicePropertyGet);
            }


            public void DefineImplementationProperty()

            {
                // Implementation = get_RawImpl/set_RawImpl

                var implementationPropertyGet = _typeBuilder.DefineMethod(
                    "get_RawImpl",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig |
                    MethodAttributes.Final,
                    typeof (object),
                    Type.EmptyTypes);

                {
                    var g = implementationPropertyGet.GetILGenerator();

                    // return _impl;

                    g.Emit(OpCodes.Ldarg_0);

                    g.Emit(OpCodes.Ldfld, _implField);

                    g.Emit(OpCodes.Ret);
                }

                var implementationPropertySet = _typeBuilder.DefineMethod(
                    "set_RawImpl",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig |
                    MethodAttributes.Final,
                    typeof (void),
                    new[] {typeof (object)});

                {
                    var g = implementationPropertySet.GetILGenerator();

                    // _impl = (T)value;

                    g.Emit(OpCodes.Ldarg_0);

                    g.Emit(OpCodes.Ldarg_1);

                    g.Emit(OpCodes.Castclass, _implField.FieldType);

                    g.Emit(OpCodes.Stfld, _implField);

                    g.Emit(OpCodes.Ret);
                }
            }


            public void ImplementInterface()

            {
                // Starts with properties and events.

                ImplementProperties();

                ImplementEvents();

                // Non processed methods are then implemented.

                ImplementRemainingMethods();
            }


            public void FinalizeStatic()
            {
                _typeBuilder.CreateType();
            }


            void ImplementProperties()
            {
                foreach (var p in ReflectionHelper.GetFlattenProperties(_definition.TypeInterface))
                {
                    var mGet = p.GetGetMethod(true);

                    if(mGet != null)

                    {
                        var optGet = _definition.GetPropertyMethodOptions(p, mGet);

                        GenerateInterceptor(mGet, optGet);
                    }

                    var mSet = p.GetSetMethod(true);

                    if(mSet != null)

                    {
                        var optSet = _definition.GetPropertyMethodOptions(p, mSet);

                        GenerateInterceptor(mSet, optSet);
                    }
                }
            }


            void ImplementEvents()

            {
                foreach (var e in ReflectionHelper.GetFlattenEvents(_definition.TypeInterface))

                {
                    var optEvent = _definition.GetEventOptions(e);

                    DefineEventSupport(e, optEvent);
                }
            }


            void DefineEventSupport(EventInfo e, ProxyOptions generationOptions)

            {
                // Defines the hook field: Delegate _dXXX;

                var dField = _typeBuilder.DefineField("_d" + e.Name, typeof (Delegate), FieldAttributes.Private);


                // Defines the event field: <EventHandler> _hookXXX;

                var hField = _typeBuilder.DefineField("_hook" + e.Name, e.EventHandlerType,
                    FieldAttributes.Private);


                var eventMetaRef = RegisterRef(_eRefs, e);


                // Implements our hook method.

                MethodBuilder mHookB;

                {
                    var mCall = e.EventHandlerType.GetMethod("Invoke");

                    var parameters = ReflectionHelper.CreateParametersType(mCall.GetParameters());

                    mHookB = _typeBuilder.DefineMethod("_realService_" + e.Name,
                        MethodAttributes.Private | MethodAttributes.Final, CallingConventions.HasThis, typeof (void),
                        parameters);

                    {
                        SetDebuggerStepThroughAttribute(mHookB);

                        var g = mHookB.GetILGenerator();

                        var logOptions = g.DeclareLocal(typeof (ServiceLogEventOptions));

                        var logger = g.DeclareLocal(typeof (LogEventEntry));


                        g.Emit(OpCodes.Ldarg_0);

                        g.LdInt32(eventMetaRef);

                        g.Emit(OpCodes.Ldloca_S, logger);

                        g.Emit(OpCodes.Ldloca_S, logOptions);

                        string getLoggerName;

                        switch (generationOptions.RuntimeCheckStatus)

                        {
                            case ProxyOptions.CheckStatus.None:
                                getLoggerName = "GetLoggerEventForAnyCall";
                                break;

                            case ProxyOptions.CheckStatus.NotDisabled:
                                getLoggerName = "GetLoggerEventForNotDisabledCall";
                                break;

                            default:
                                getLoggerName = "GetLoggerEventForRunningCall";
                                break; //ProxyOptions.CheckStatus.Running
                        }

                        g.EmitCall(OpCodes.Call,
                            _definition.ProxyBase.GetMethod(getLoggerName,
                                BindingFlags.NonPublic | BindingFlags.Instance), null);


                        var doRaise = g.DefineLabel();

                        g.Emit(OpCodes.Brtrue_S, doRaise);

                        g.Emit(OpCodes.Ret);

                        g.MarkLabel(doRaise);


                        var client = g.DeclareLocal(typeof (Delegate));

                        var exception = generationOptions.CatchExceptions
                            ? g.DeclareLocal(typeof (Exception))
                            : null;

                        var list = g.DeclareLocal(typeof (Delegate[]));

                        var listLength = g.DeclareLocal(typeof (int));

                        var index = g.DeclareLocal(typeof (int));

                        // Maps actual parameters.

                        for (var i = 0; i < parameters.Length; ++i)

                        {
                            if(parameters[i].IsAssignableFrom(_definition.TypeInterface))

                            {
                                g.LdArg(i + 1);

                                g.Emit(OpCodes.Ldarg_0);

                                g.Emit(OpCodes.Ldfld, _implField);

                                var notTheSender = g.DefineLabel();

                                g.Emit(OpCodes.Bne_Un_S, notTheSender);

                                g.Emit(OpCodes.Ldarg_0);

                                g.StArg(i + 1);

                                g.MarkLabel(notTheSender);
                            }
                        }

                        // Should we log parameters?

                        if(parameters.Length > 0)

                        {
                            var skipLogParam = g.DefineLabel();

                            g.LdLoc(logOptions);

                            g.LdInt32((int) ServiceLogEventOptions.LogParameters);

                            g.Emit(OpCodes.And);

                            g.Emit(OpCodes.Brfalse, skipLogParam);


                            var paramsArray = g.DeclareLocal(typeof (object[]));

                            g.CreateObjectArrayFromInstanceParameters(paramsArray, parameters);


                            g.LdLoc(logger);

                            g.LdLoc(paramsArray);

                            g.Emit(OpCodes.Stfld,
                                typeof (LogEventEntry).GetField("_parameters",
                                    BindingFlags.Instance | BindingFlags.NonPublic));


                            g.MarkLabel(skipLogParam);
                        }


                        // Gets all the delegate to call in list.

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, dField);

                        g.EmitCall(OpCodes.Callvirt, _delegateGetInvocationList, null);

                        g.StLoc(list);

                        // listLength = list.Length;

                        g.LdLoc(list);

                        g.Emit(OpCodes.Ldlen);

                        g.Emit(OpCodes.Conv_I4);

                        g.StLoc(listLength);

                        // index = 0;

                        g.Emit(OpCodes.Ldc_I4_0);

                        g.StLoc(index);


                        var beginOfLoop = g.DefineLabel();

                        var endOfLoop = g.DefineLabel();

                        g.Emit(OpCodes.Br_S, endOfLoop);


                        g.MarkLabel(beginOfLoop);

                        // client = list[index];

                        g.LdLoc(list);

                        g.LdLoc(index);

                        g.Emit(OpCodes.Ldelem_Ref);

                        g.StLoc(client);


                        if(generationOptions.CatchExceptions)

                        {
                            g.BeginExceptionBlock();
                        }

                        g.LdLoc(client);

                        g.Emit(OpCodes.Castclass, e.EventHandlerType);

                        g.RepushActualParameters(false, parameters.Length);

                        g.EmitCall(OpCodes.Callvirt, mCall, null);


                        if(generationOptions.CatchExceptions)

                        {
                            var bottomOfLoop = g.DefineLabel();

                            g.Emit(OpCodes.Leave_S, bottomOfLoop);


                            g.BeginCatchBlock(typeof (Exception));

                            g.StLoc(exception);


                            g.Emit(OpCodes.Ldarg_0);

                            g.LdInt32(eventMetaRef);

                            g.LdLoc(client);

                            g.EmitCall(OpCodes.Callvirt, _delegateGetMethod, null);


                            g.LdLoc(exception);

                            g.Emit(OpCodes.Ldloca_S, logger);

                            g.EmitCall(OpCodes.Call,
                                _definition.ProxyBase.GetMethod("OnEventHandlingException",
                                    BindingFlags.NonPublic | BindingFlags.Instance), null);


                            var continueDispatch = g.DefineLabel();

                            g.Emit(OpCodes.Brtrue_S, continueDispatch);

                            g.Emit(OpCodes.Rethrow);

                            g.MarkLabel(continueDispatch);


                            g.Emit(OpCodes.Leave_S, bottomOfLoop);

                            g.EndExceptionBlock();


                            g.MarkLabel(bottomOfLoop);
                        }

                        // ++index;

                        g.LdLoc(index);

                        g.Emit(OpCodes.Ldc_I4_1);

                        g.Emit(OpCodes.Add);

                        g.StLoc(index);


                        // Checks whether we must continue the loop.

                        g.MarkLabel(endOfLoop);

                        g.LdLoc(index);

                        g.LdLoc(listLength);

                        g.Emit(OpCodes.Blt_S, beginOfLoop);


                        // if( (o & LogMethodOptions.Leave) != 0 )

                        // {

                        g.LdLoc(logOptions);

                        g.LdInt32((int) ServiceLogEventOptions.EndRaise);

                        g.Emit(OpCodes.And);

                        var skipLogPostCall = g.DefineLabel();

                        g.Emit(OpCodes.Brfalse, skipLogPostCall);


                        g.Emit(OpCodes.Ldarg_0);

                        g.LdLoc(logger);

                        g.EmitCall(OpCodes.Call,
                            _definition.ProxyBase.GetMethod("LogEndRaise",
                                BindingFlags.NonPublic | BindingFlags.Instance), null);

                        g.MarkLabel(skipLogPostCall);

                        // }


                        g.Emit(OpCodes.Ret);
                    }
                }

                // Defines the event property itself: <EventHandler> XXX;

                var eB = _typeBuilder.DefineEvent(e.Name, e.Attributes, e.EventHandlerType);


                // Implements the add_

                var mAdd = e.GetAddMethod(true);

                if(mAdd != null)

                {
                    // Registers the method to skip its processing.

                    _processedMethods.Add(mAdd);


                    var parameters = ReflectionHelper.CreateParametersType(mAdd.GetParameters());

                    var mAddB = _typeBuilder.DefineMethod(mAdd.Name,
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                        CallingConventions.HasThis, mAdd.ReturnType, parameters);

                    {
                        SetDebuggerStepThroughAttribute(mAddB);

                        var g = mAddB.GetILGenerator();


                        var dFieldOK = g.DefineLabel();


                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, dField);

                        g.Emit(OpCodes.Brtrue_S, dFieldOK);


                        var hFieldOK = g.DefineLabel();

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, hField);

                        g.Emit(OpCodes.Brtrue_S, hFieldOK);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldftn, mHookB);

                        g.Emit(OpCodes.Newobj,
                            e.EventHandlerType.GetConstructor(new[] {typeof (object), typeof (IntPtr)}));

                        g.Emit(OpCodes.Stfld, hField);


                        g.MarkLabel(hFieldOK);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, _implField);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, hField);

                        g.Emit(OpCodes.Callvirt, mAdd);


                        g.MarkLabel(dFieldOK);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, dField);

                        g.Emit(OpCodes.Ldarg_1);

                        g.Emit(OpCodes.Call, _delegateCombine);

                        g.Emit(OpCodes.Stfld, dField);


                        g.Emit(OpCodes.Ret);
                    }

                    eB.SetAddOnMethod(mAddB);
                }


                // Implements the remove_

                var mRemove = e.GetRemoveMethod(true);

                if(mRemove != null)

                {
                    // Registers the method to skip its processing.

                    _processedMethods.Add(mRemove);


                    var parameters = ReflectionHelper.CreateParametersType(mRemove.GetParameters());

                    var mRemoveB = _typeBuilder.DefineMethod(mRemove.Name,
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                        CallingConventions.HasThis,
                        mRemove.ReturnType, parameters);

                    {
                        SetDebuggerStepThroughAttribute(mRemoveB);

                        var g = mRemoveB.GetILGenerator();


                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, dField);

                        g.Emit(OpCodes.Ldarg_1);

                        g.Emit(OpCodes.Call, _delegateRemove);

                        g.Emit(OpCodes.Stfld, dField);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, dField);

                        var end = g.DefineLabel();

                        g.Emit(OpCodes.Brtrue_S, end);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, _implField);

                        g.Emit(OpCodes.Ldarg_0);

                        g.Emit(OpCodes.Ldfld, hField);

                        g.Emit(OpCodes.Callvirt, mRemove);

                        g.MarkLabel(end);


                        g.Emit(OpCodes.Ret);
                    }

                    eB.SetRemoveOnMethod(mRemoveB);
                }
            }


            void ImplementRemainingMethods()

            {
                // For each methods in definition.TypeInterface...

                foreach (var m in ReflectionHelper.GetFlattenMethods(_definition.TypeInterface))

                {
                    if(!_processedMethods.Contains(m))

                    {
                        var generationOptions = _definition.GetMethodOptions(m);

                        GenerateInterceptor(m, generationOptions);
                    }
                }
            }


            /// <summary>
            ///     Registers an index to a <see cref="MemberInfo" />.
            /// </summary>
            int RegisterRef<T>(List<T> reg, T element)

            {
                var i = reg.IndexOf(element);

                if(i < 0)

                {
                    i = reg.Count;

                    reg.Add(element);
                }

                return i;
            }


            /// <summary>
            ///     Generates the exact signature and the code that relays the call
            ///     to the _impl corresponding method.
            /// </summary>
            void GenerateInterceptor(MethodInfo m, ProxyOptions generationOptions)

            {
                // Registers the method.

                Debug.Assert(m != null && !_processedMethods.Contains(m));

                _processedMethods.Add(m);


                Type[] parameters;

                var mB = CreateInterfaceMethodBuilder(_typeBuilder, m, out parameters);

                SetDebuggerStepThroughAttribute(mB);


                var metaRef = RegisterRef(_mRefs, m);

                #region Body generation

                {
                    var g = mB.GetILGenerator();


                    var logOption = g.DeclareLocal(typeof (ServiceLogMethodOptions));

                    var logger = g.DeclareLocal(typeof (LogMethodEntry));


                    // The retValue local is used only if we must intercept 

                    // the return value (of course, if there is a return value).

                    LocalBuilder retValue = null;

                    if(m.ReturnType != typeof (void))

                    {
                        retValue = g.DeclareLocal(m.ReturnType);
                    }


                    g.Emit(OpCodes.Ldarg_0);

                    g.LdInt32(metaRef);

                    g.Emit(OpCodes.Ldloca_S, logger);


                    string getLoggerName;

                    switch (generationOptions.RuntimeCheckStatus)

                    {
                        case ProxyOptions.CheckStatus.None:
                            getLoggerName = "GetLoggerForAnyCall";
                            break;

                        case ProxyOptions.CheckStatus.NotDisabled:
                            getLoggerName = "GetLoggerForNotDisabledCall";
                            break;

                        default:
                            getLoggerName = "GetLoggerForRunningCall";
                            break; //ProxyOptions.CheckStatus.Running
                    }

                    g.EmitCall(OpCodes.Call,
                        _definition.ProxyBase.GetMethod(getLoggerName, BindingFlags.NonPublic | BindingFlags.Instance),
                        null);


                    g.StLoc(logOption);

                    if(parameters.Length > 0)

                    {
                        var skipLogParam = g.DefineLabel();

                        g.LdLoc(logOption);

                        g.LdInt32((int) ServiceLogMethodOptions.LogParameters);

                        g.Emit(OpCodes.And);

                        g.Emit(OpCodes.Brfalse, skipLogParam);


                        var paramsArray = g.DeclareLocal(typeof (object[]));

                        g.CreateObjectArrayFromInstanceParameters(paramsArray, parameters);


                        g.LdLoc(logger);

                        g.LdLoc(paramsArray);

                        g.Emit(OpCodes.Stfld,
                            typeof (LogMethodEntry).GetField("_parameters",
                                BindingFlags.Instance | BindingFlags.NonPublic));


                        g.MarkLabel(skipLogParam);
                    }

                    LocalBuilder exception = null;

                    if(generationOptions.CatchExceptions)

                    {
                        exception = g.DeclareLocal(typeof (Exception));

                        g.BeginExceptionBlock();
                    }


                    // Pushes the _impl field on the stack.

                    g.Emit(OpCodes.Ldarg_0);

                    g.Emit(OpCodes.Ldfld, _implField);

                    // Pushes all the parameters on the stack.

                    g.RepushActualParameters(false, parameters.Length);

                    g.EmitCall(OpCodes.Callvirt, m, null);


                    // if( (o & LogMethodOptions.Leave) != 0 )

                    g.LdLoc(logOption);

                    g.LdInt32((int) ServiceLogMethodOptions.Leave);

                    g.Emit(OpCodes.And);

                    var skipLogPostCall = g.DefineLabel();

                    g.Emit(OpCodes.Brfalse, skipLogPostCall);


                    // {

                    if(retValue == null)

                    {
                        g.Emit(OpCodes.Ldarg_0);

                        g.LdLoc(logger);

                        g.EmitCall(OpCodes.Call,
                            _definition.ProxyBase.GetMethod("LogEndCall", BindingFlags.NonPublic | BindingFlags.Instance),
                            null);
                    }

                    else

                    {
                        var skipLogWithValue = g.DefineLabel();

                        g.LdLoc(logOption);

                        g.LdInt32((int) ServiceLogMethodOptions.LogReturnValue);

                        g.Emit(OpCodes.And);

                        g.Emit(OpCodes.Brfalse, skipLogWithValue);


                        // Save retValue.

                        g.StLoc(retValue);


                        g.Emit(OpCodes.Ldarg_0);

                        g.LdLoc(logger);

                        g.LdLoc(retValue);

                        if(m.ReturnType.IsGenericParameter || m.ReturnType.IsValueType)

                        {
                            g.Emit(OpCodes.Box, m.ReturnType);
                        }

                        g.EmitCall(OpCodes.Call,
                            _definition.ProxyBase.GetMethod("LogEndCallWithValue",
                                BindingFlags.NonPublic | BindingFlags.Instance), null);


                        // Repush retValue and go to end.

                        g.LdLoc(retValue);

                        g.Emit(OpCodes.Br_S, skipLogPostCall);


                        // Just call LogEndCall without return value management (stack is okay).

                        g.MarkLabel(skipLogWithValue);

                        g.Emit(OpCodes.Ldarg_0);

                        g.LdLoc(logger);

                        g.EmitCall(OpCodes.Call,
                            _definition.ProxyBase.GetMethod("LogEndCall", BindingFlags.NonPublic | BindingFlags.Instance),
                            null);
                    }

                    // }

                    g.MarkLabel(skipLogPostCall);


                    if(generationOptions.CatchExceptions)

                    {
                        var end = g.DefineLabel();

                        g.Emit(OpCodes.Br_S, end);


                        g.BeginCatchBlock(typeof (Exception));

                        g.StLoc(exception);


                        // if( (o & LogMethodOptions.LogError) != 0 )

                        g.LdLoc(logOption);

                        g.LdInt32((int) ServiceLogMethodOptions.LogError);

                        g.Emit(OpCodes.And);

                        var skipExceptionCall = g.DefineLabel();

                        g.Emit(OpCodes.Brfalse, skipExceptionCall);


                        g.Emit(OpCodes.Ldarg_0);

                        g.LdInt32(metaRef);

                        g.LdLoc(exception);

                        g.LdLoc(logger);

                        g.EmitCall(OpCodes.Call,
                            _definition.ProxyBase.GetMethod("OnCallException",
                                BindingFlags.NonPublic | BindingFlags.Instance), null);


                        g.MarkLabel(skipExceptionCall);


                        g.Emit(OpCodes.Rethrow);

                        g.EndExceptionBlock();


                        g.MarkLabel(end);
                    }

                    g.Emit(OpCodes.Ret);
                }

                #endregion
            }


            void SetDebuggerStepThroughAttribute(MethodBuilder mB)

            {
                var ctor = typeof (DebuggerStepThroughAttribute).GetConstructor(Type.EmptyTypes);

                var attr = new CustomAttributeBuilder(ctor, new object[0]);

                mB.SetCustomAttribute(attr);
            }


            /// <summary>
            ///     Creates a <see cref="MethodBuilder" /> for a given method.
            ///     Handles generic parameters on the method.
            /// </summary>
            /// <param name="typeBuilder"></param>
            /// <param name="m"></param>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public static MethodBuilder CreateInterfaceMethodBuilder(TypeBuilder typeBuilder, MethodInfo m,
                out Type[] parameters)

            {
                // Initializes the signature with only its name, attributes and calling conventions first.

                var mB = typeBuilder.DefineMethod(
                    m.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig |
                    MethodAttributes.Final,
                    CallingConventions.HasThis);


                parameters = ReflectionHelper.CreateParametersType(m.GetParameters());

                // If it is a Generic method definition (since we are working with an interface, 

                // it can not be a Generic closed nor opened method).

                if(m.IsGenericMethodDefinition)

                {
                    var genArgs = m.GetGenericArguments();

                    Debug.Assert(genArgs.Length > 0);

                    var names = new string[genArgs.Length];

                    for (var i = 0; i < names.Length; ++i) names[i] = genArgs[i].Name;

                    // Defines generic parameters.

                    var genTypes = mB.DefineGenericParameters(names);

                    for (var i = 0; i < names.Length; ++i)

                    {
                        var source = genArgs[i];

                        var target = genTypes[i];

                        target.SetGenericParameterAttributes(source.GenericParameterAttributes);

                        var sourceConstraints = source.GetGenericParameterConstraints();

                        var interfaces = new List<Type>();

                        foreach (var c in sourceConstraints)

                        {
                            if(c.IsClass) target.SetBaseTypeConstraint(c);

                            else interfaces.Add(c);
                        }

                        target.SetInterfaceConstraints(interfaces.ToArray());
                    }
                }

                // Now that generic parameters have been defined, configures the signature.

                mB.SetReturnType(m.ReturnType);

                mB.SetParameters(parameters);

                return mB;
            }
        }

        public static void DefineNewProxyAssembly(string generatedProxyAssemblyName)
        {
            if(generatedProxyAssemblyName.Equals(_assemblyName?.Name))
                throw new InvalidOperationException(nameof(generatedProxyAssemblyName) + " already initialized");

            var libDir = Util.ProxyAssemblyFolderPath;
            _assemblyName = new AssemblyName(generatedProxyAssemblyName)
            {
                Version = new Version(1, 0, 0, 0),
                CodeBase = libDir
            };

            _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.RunAndSave, libDir);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("ProxiesModule", _assemblyName.Name + ".dll", true);
        }

        public static void WriteProxyAssemblyToDisk()
        {
            _assemblyBuilder.Save(_assemblyName.Name + ".dll");
        }
    }
}