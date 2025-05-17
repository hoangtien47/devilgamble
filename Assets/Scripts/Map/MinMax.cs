using UnityEngine;

namespace Map
{
    [System.Serializable]
    public class FloatMinMax
    {
        public float min;
        public float max;

        public float GetValue()
        {
            return Random.Range(min, max);
        }
    }
}

namespace Map
{
    [System.Serializable]
    public class IntMinMax
    {
        public int min;
        public int max;

        public int GetValue(System.Random rnd = null)
        {
            if (rnd == null)
                return UnityEngine.Random.Range(min, max + 1);
            return rnd.Next(min, max + 1);
        }
    }
}
