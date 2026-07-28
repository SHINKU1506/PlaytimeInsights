namespace PlaytimeInsights.ViewModels
{
    public sealed class RefreshReentrancyGuard
    {
        public bool IsActive { get; private set; }

        public bool TryEnter()
        {
            if (IsActive)
            {
                return false;
            }

            IsActive = true;
            return true;
        }

        public void Exit()
        {
            IsActive = false;
        }
    }
}
