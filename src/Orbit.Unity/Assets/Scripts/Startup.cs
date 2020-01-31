using System;

using Microsoft.Extensions.DependencyInjection;

using Orbit.Util;

using UnityEditor;

using UnityEngine;

[InitializeOnLoad]
public class Startup : MonoBehaviour
{
    private static IServiceProvider ServiceProvider => OrbitServiceProvider.Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Initialize()
    {
        Debug.Log("Calling Startup.Initialize");
        var dataGenerator = ServiceProvider.GetRequiredService<IDataGenerator>();

        dataGenerator.Start();
        dataGenerator.Started += (s, e) => Debug.Log("Data generator started");
    }
}