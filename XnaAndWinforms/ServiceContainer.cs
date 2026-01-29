#region File Description
//-----------------------------------------------------------------------------
// ServiceContainer.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
#endregion

namespace XnaAndWinforms;

/// <summary>
/// Container class implements the IServiceProvider interface. This is used
/// to pass shared services between different components, for instance the
/// ContentManager uses it to locate the IGraphicsDeviceService implementation.
/// </summary>
public class ServiceContainer : IServiceProvider
{
    Dictionary<Type, object> services = new Dictionary<Type, object>();


    /// <summary>
    /// Adds a new service to the collection.
    /// </summary>
    public void AddService<T>(T service)
    {
        if(service != null)
        {
            services.Add(typeof(T), service);
        }
    }


    /// <summary>
    /// Looks up the specified service.
    /// </summary>
    public object? GetService(Type serviceType)
    {
        services.TryGetValue(serviceType, out var service);

        return service;
    }
}
