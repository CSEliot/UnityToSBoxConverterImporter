internal static class CollectionExtensions
{
	public static T Largest<T>(this IEnumerable<T> collection) where T : IComparable<T>
	{
		if ( collection == default || !collection.Any() ) return default;

		T largest = collection.First();
		foreach (T item in collection)
		{
			if(item.CompareTo(largest) > 0)
			{
				largest = item;
			}
		}
		return largest;
	}

	public static T LargestBy<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector ) where TKey : IComparable<TKey>
	{
		if ( collection == default || !collection.Any() ) return default;

		T largestItem = collection.First();
		TKey largestValue = keySelector(largestItem);
		foreach ( T item in collection )
		{
			TKey value = keySelector( item );
			if ( value.CompareTo( largestValue ) > 0 )
			{
				largestValue = value;
				largestItem = item;
			}
		}
		return largestItem;
	}

	public static T Smallest<T>( this IEnumerable<T> collection ) where T : IComparable<T>
	{
		if ( collection == default || !collection.Any() ) return default;

		T largest = collection.First();
		foreach ( T item in collection )
		{
			if ( item.CompareTo( largest ) < 0 )
			{
				largest = item;
			}
		}
		return largest;
	}

	public static T SmallestBy<T, TKey>( this IEnumerable<T> collection, Func<T, TKey> keySelector ) where TKey : IComparable<TKey>
	{
		if ( collection == default || !collection.Any() ) return default;

		T largestItem = collection.First();
		TKey largestValue = keySelector(largestItem);
		foreach ( T item in collection )
		{
			TKey value = keySelector( item );
			if ( value.CompareTo( largestValue ) < 0 )
			{
				largestValue = value;
				largestItem = item;
			}
		}
		return largestItem;
	}

	public static TValue GetValueOrDefault<TKey, TValue>( this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default )
	{
		return dictionary.TryGetValue( key, out var value ) ? value : defaultValue;
	}
}
