namespace ShocoSharp
{
    /// <summary>
    /// Packing scheme for shoco compression
    /// </summary>
    public class ShocoPack
    {
        public readonly byte Header;
        public readonly int BytesPacked;
        public readonly int BytesUnpacked;
        internal readonly int[] offsets;
        internal readonly int[] masks;

        public int[] Offsets => offsets.MakeCopy();
        public int[] Masks => masks.MakeCopy();

        public ShocoPack(byte Header, int BytesPacked, int BytesUnpacked, int[] Offsets, int[] Masks)
        {
            this.Header = Header;
            this.BytesPacked = BytesPacked;
            this.BytesUnpacked = BytesUnpacked;
            offsets = Offsets;
            masks = Masks;
        }
    }
}
