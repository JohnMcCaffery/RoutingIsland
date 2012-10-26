namespace common.Queue {
    // TODO IMPLEMENTED
    public class AsynchQueueFactory : IAsynchQueueFactory {
        #region IAsynchQueueFactory Members

        public IAsynchQueue MakeQueue() {
            return new AsynchQueue();
        }

        #endregion
    }
}