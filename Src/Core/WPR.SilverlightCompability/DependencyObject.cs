using System.Collections.Generic;

namespace WPR.SilverlightCompability
{
    public class DependencyObject
    {
        private Dictionary<DependencyProperty, object?>? _values;

        public object? GetValue(DependencyProperty dp)
        {
            if (_values != null && _values.TryGetValue(dp, out var v))
                return v;
            return dp.DefaultMetadata.DefaultValue;
        }

        public void SetValue(DependencyProperty dp, object? value)
        {
            object? oldValue = GetValue(dp);
            if (Equals(oldValue, value))
                return;

            _values ??= new Dictionary<DependencyProperty, object?>();
            _values[dp] = value;

            dp.DefaultMetadata.PropertyChangedCallback?.Invoke(
                this, new DependencyPropertyChangedEventArgs(dp, oldValue, value));
        }

        public void ClearValue(DependencyProperty dp)
        {
            if (_values == null || !_values.TryGetValue(dp, out var oldValue))
                return;

            _values.Remove(dp);
            object? newValue = dp.DefaultMetadata.DefaultValue;

            if (!Equals(oldValue, newValue))
            {
                dp.DefaultMetadata.PropertyChangedCallback?.Invoke(
                    this, new DependencyPropertyChangedEventArgs(dp, oldValue, newValue));
            }
        }
    }
}
