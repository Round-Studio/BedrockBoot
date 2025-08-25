using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockBoot.Models.Classes.Listen
{
    public static class VariableListenerExtension
    {
        /// <summary>
        /// 为变量添加监听器
        /// </summary>
        public static Listenable<T> Listen<T>(this T value, Action<T, T> onChange = null)
        {
            return new Listenable<T>(value, onChange);
        }
    }

    /// <summary>
    /// 可监听的变量包装类
    /// </summary>
    public class Listenable<T>
    {
        private T _value;
        private Action<T, T> _onChange;
        private List<Action<T, T>> _listeners = new List<Action<T, T>>();

        public Listenable(T initialValue, Action<T, T> onChange = null)
        {
            _value = initialValue;
            if (onChange != null)
            {
                _listeners.Add(onChange);
            }
        }

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    T oldValue = _value;
                    _value = value;
                    NotifyListeners(oldValue, value);
                }
            }
        }

        /// <summary>
        /// 添加监听器
        /// </summary>
        public Listenable<T> ChangeListener(Action<T, T> listener)
        {
            _listeners.Add(listener);
            return this;
        }

        /// <summary>
        /// 移除监听器
        /// </summary>
        public void RemoveListener(Action<T, T> listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// 清空所有监听器
        /// </summary>
        public void ClearListeners()
        {
            _listeners.Clear();
        }

        private void NotifyListeners(T oldValue, T newValue)
        {
            foreach (var listener in _listeners)
            {
                listener(oldValue, newValue);
            }
        }

        // 隐式转换，使得 Listenable<T> 可以当作 T 使用
        public static implicit operator T(Listenable<T> listenable) => listenable.Value;
        public static implicit operator Listenable<T>(T value) => new Listenable<T>(value);
    }
}
