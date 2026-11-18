namespace UnderGrind.Inventory
{
    public readonly struct InventoryOperationResult
    {
        public int Requested { get; }
        public int Processed { get; }
        public int Remaining => Requested - Processed;
        public bool Completed => Remaining == 0;

        public InventoryOperationResult(int requested, int processed)
        {
            Requested = requested;
            Processed = processed;
        }
    }
}