namespace Benday.AzureDevOpsUtil.Api
{
    public class WorkItemStateTransition
    {
        /// <summary>
        /// Initializes a new instance of the WorkItemStateTransition class.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public WorkItemStateTransition(string from, string to)
        {
            if (string.IsNullOrEmpty(to))
                throw new ArgumentException("to is null or empty.", nameof(to));

            From = from ?? throw new ArgumentNullException(nameof(from), "Argument cannot be null.");
            To = to;
            Reason = "State updated";
        }

        public string From { get; }
        public string To { get; }
        public string Reason { get; }

        public string ToXml()
        {
            var template = "<TRANSITION from=\"{0}\" to=\"{1}\"><REASONS><DEFAULTREASON value=\"{2}\" /></REASONS></TRANSITION>";

            return string.Format(template, From, To, Reason);
        }
    }
}
