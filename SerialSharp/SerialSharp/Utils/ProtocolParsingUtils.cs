namespace SerialSharp.Utils
{
    internal class ProtocolParsingUtils
    {
        public static int ReadLengthFromIndices(byte[] buffer, int[] indices, bool isBigEndian)
        {
            if (indices.Length == 0) return 0;

            var result = 0;
            if (isBigEndian)
            {
                result = indices.Aggregate(result, (current, index) => (current << 8) | buffer[index]);
            }
            else
            {
                for (var i = indices.Length - 1; i >= 0; i--)
                {
                    result = (result << 8) | buffer[indices[i]];
                }
            }

            return result;
        }
    }
}
