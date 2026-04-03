using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeyondStorage.Scripts.Data;

internal class StorageTargetAdapter<T> where T : class
{
    private readonly StorageSourceAdapter<T> _source;
    private readonly float _distance;

    public StorageTargetAdapter(StorageSourceAdapter<T> source, float distance)
    {
        _source = source;
        _distance = distance;
    }

    public float Distance => _distance;

    private void BuildDescriptorMaps()
    {
        // We need to build maps about metadata. This is currently not efficient at all.

    }

    internal bool ContainsItem(ItemStack itemStack)
    {
        BuildDescriptorMaps();
        return false;
    }

    internal string GetTargetName()
    {
        if (_source == null)
        {
            return "null source has no name";
        }

        return _source.GetName();
    }

    internal ItemStack[] GetAllSlotItemStacks()
    {
        if (_source == null)
        {
            return [];
        }

        return _source.GetAllSlotItemsStacks();
    }
}
