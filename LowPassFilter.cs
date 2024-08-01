using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PimaxCrystalAdvanced
{
    public class LowPassFilter
    {
        private readonly float[] _samples;
        private int _index;

        public LowPassFilter(int count)
        {
            _samples = new float[count - 1];
            for (var i = 0; i < count - 1; i++)
            {
                _samples[i] = 0.0f;
            }
        }

        private float Sum()
        {
            float weight = 0;
            foreach (var sample in _samples)
            {
                weight += sample;
            }
            return weight;
        }

        public float FilterValue(float newValue)
        {
            _index++;
            if (_samples.Length == _index)
                _index = 0;

            _samples[_index] = newValue;

            return Sum() / _samples.Length;
        }
    }
}
