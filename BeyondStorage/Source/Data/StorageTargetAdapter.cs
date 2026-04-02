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

    public StorageSourceAdapter<T> Source => _source;
    public float Distance => _distance;
}
