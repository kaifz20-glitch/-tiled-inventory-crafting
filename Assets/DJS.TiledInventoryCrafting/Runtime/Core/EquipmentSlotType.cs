namespace DJS.TiledInventoryCrafting
{
    /// <summary>Which equipment slot an item occupies. <see cref="None"/> means the item
    /// cannot be equipped and lives only in regular inventory grids.</summary>
    public enum EquipmentSlotType
    {
        None,
        Head,
        Chest,
        Legs,
        Weapon,
        Accessory
    }
}
