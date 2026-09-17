using System;
using Il2CppInterop.Runtime.Injection;
using Il2CppScheduleOne.Employees;
using UnityEngine;

namespace VehicleHandlers.Employees
{
    public sealed class HandlerEmployee : MonoBehaviour
    {
        public HandlerEmployee(IntPtr pointer)
            : base(pointer)
        {
        }

        public static void RegisterIl2CppType()
        {
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp<HandlerEmployee>())
            {
                ClassInjector.RegisterTypeInIl2Cpp<HandlerEmployee>();
            }
        }

        public void Awake()
        {
            Packager donor = GetComponent<Packager>();
            if (donor != null)
            {
                HandlerEmployeeRegistry.Register(this, donor);
            }
        }

        public void OnDestroy()
        {
            HandlerEmployeeRegistry.Detach(this);
        }
    }
}
