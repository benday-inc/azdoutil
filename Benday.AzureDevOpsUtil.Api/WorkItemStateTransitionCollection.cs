namespace Benday.AzureDevOpsUtil.Api
{
    public class WorkItemStateTransitionCollection : List<WorkItemStateTransition>
    {
        public bool Contains(string from, string to)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from), "Argument cannot be null.");
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to), "Argument cannot be null.");
            }

            if (Count == 0)
            {
                return false;
            }
            else
            {
                var match = this.Where(x => x.From == from && x.To == to).FirstOrDefault();

                if (match == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        internal void Add(string from, string to)
        {
            Add(new WorkItemStateTransition(from, to));
        }
    }
}
