using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;
using Orbit;
using Orbit.Util;
using UnityEngine.UI;
using Orbit.Models;

public class FetchWater : MonoBehaviour
{
    Text textComponent;

    // Start is called before the first frame update
    void Start()
    {
        this.textComponent = GetComponent<Text>();
        InvokeRepeating(nameof(HandleUpdate), 0, 2f);
    }

    private const string format = "Tank level: {0}";

    public async void HandleUpdate()
    {
        Debug.Log("Calling Update");
        using (var scope = OrbitServiceProvider.Instance.CreateScope())
        {
            var comp = scope.ServiceProvider.GetService<IMonitoredComponent<WaterProcessorData>>();
            var next = await comp.GetLatestReportAsync();
            if (next == null)
            {
                Debug.Log("No report found");
                this.textComponent.text = string.Format(format, 0);
                return;
            }
            string value = string.Format(format, next.ProductTankLevel);

            Debug.Log(value);
            this.textComponent.text = value;
        }
    }
}
