using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System;

public static class TypeUtils {
  public static List<Type> GetTypesOf<T>(bool isClass = true, bool isAbstract = false) {
    var types = new List<Type>();
    if (isClass && !isAbstract) {
      types = Assembly.GetAssembly(typeof(T))
            .GetTypes().Where(p => typeof(T).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToList();
    }

    // TODO: implement all isClass cases
    return types;
  }

  public static List<T> GetInstancesOf<T>() where T : class {
    var instances = new List<T>();
    GetTypesOf<T>().ForEach(type => {
      instances.Add(Activator.CreateInstance(type) as T);
    });

    return instances;
  }
}