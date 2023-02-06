namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //Used to signify that this wrapper will want to write to a common DataSource. 
    //We need to detect this so that we don't have multiple wrappers for that same DataSource as the 
    //Acquire/Release flow will error when two or more wrappers call Acquire before calling Release.
    internal interface IDataStreamPendingAccessWrapper
    {
    }
}
