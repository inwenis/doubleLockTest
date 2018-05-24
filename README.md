This repo shows that you need to use doule locks if you are using multiple threads to read/remove/add to a collection.

The issue with `ProcessorCacheWithSingleLock` is that in `Get()` after making sure the entry exists by calling `if (Exists(id))` a lot can happen before execution reaches 
```
_lock.EnterReadLock();
var processor = _dictionary[id];
_lock.ExitReadLock();
```
and the entry we're trying to read may be removed.

If you hear the console beep and see red errors saying `the key not found exception was thrown` it means that the test succesfully showed that using `ProcessorCacheWithSingleLock` failed.