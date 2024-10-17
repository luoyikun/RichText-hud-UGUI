using System;
using UnityEngine;

namespace MixFramework
{
    /// <summary>
    /// 非GameObject单例模板，线程安全
    /// </summary>
    /// <typeparam name="T">T需有构造</typeparam>
    public class Singleton<T> where T : new()
    {
        private static T _self;
        private static readonly object _lockObj;

        static Singleton()
        {
            _lockObj = new object();
        }

        public static T Instance
        {
            get
            {
                if (_self == null)
                {
                    lock (_lockObj)
                    {
                        if (_self == null)
                        {
                            _self = (default(T) == null) ? Activator.CreateInstance<T>() : default(T);
                        }
                    }
                }

                return _self;
            }
        }
    }

    /// <summary>
    /// GameObject单例模板 线程安全
    /// 该对象会在场景中以Gameobject物件的形式出现
    /// </summary>
    /// <typeparam name="T">T限定为MonoBehaviour</typeparam>
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _self;
        private static readonly object _lockObj;

        static SingletonMono()
        {
            _lockObj = new object();
        }

        public static T Instance
        {
            get
            {
                if (_self == null)
                {
                    lock (_lockObj)
                    {
                        if (_self == null)
                        {
                            GameObject go = null;
                            if (Application.platform == RuntimePlatform.WindowsEditor)
                            {
                                go = new GameObject(typeof(T).ToString()); }
                            else {
                                go = new GameObject();
                            }
                            _self = go.AddComponent<T>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }

                return _self;
            }
        }

        public  virtual void Init()
        {
            
        }
    }
}
