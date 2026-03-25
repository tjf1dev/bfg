using Spectre.Console;

public class EmptySpinner : Spinner
{
        public override TimeSpan Interval => TimeSpan.FromMilliseconds(0);
        public override bool IsUnicode => false;
        public override IReadOnlyList<string> Frames => new List<string>
            {
                    "",
            };
}