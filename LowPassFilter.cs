using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PimaxCrystalAdvanced
{
    public class LowPassFilter
    {

        private readonly float[] samples;
        private int index = 0;

        private float _Value;

        public float Value
        {
            get => _Value;
            set => FilterValue(ref value);
        }

        public LowPassFilter(int count)
        {
            samples = new float[count - 1];
            for (int i = 0; i < count - 1; i++)
            {
                samples[i] = 0.0f;
            }
        }

        private float Sum()
        {
            float weight = 0;
            foreach (var sample in samples)
            {
                weight += sample;
            }
            return weight;
        }


        public void FilterValue(ref float NewValue)
        {
            _Value = Sum() / samples.Length;

            index++;
            if (samples.Length == index)
                index = 0;

            samples[index] = NewValue;
        }
    }
}
