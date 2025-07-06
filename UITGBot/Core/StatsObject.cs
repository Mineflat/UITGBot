namespace UITGBot.Core
{
    public class StatsObject
    {
        public int botActionsCount { get; set; }
        public int botActiveActionsCount { get; set; }
        public int botMessagesReceived { get; set; } = 0;
        public int botMessagesProccessed { get; set; } = 0;


        public int ActionsCountTypeOf_full_text { get; set; } = 0;
        public int ActionsCountTypeOf_file { get; set; } = 0;
        public int ActionsCountTypeOf_image { get; set; } = 0;
        public int ActionsCountTypeOf_script { get; set; } = 0;
        public int ActionsCountTypeOf_random_text { get; set; } = 0;
        public int ActionsCountTypeOf_random_file { get; set; } = 0;
        public int ActionsCountTypeOf_random_image { get; set; } = 0;
        public int ActionsCountTypeOf_random_script { get; set; } = 0;
        public int ActionsCountTypeOf_remote_file { get; set; } = 0;
        public int ActionsCountTypeOf_simple { get; set; } = 0;
    }
}