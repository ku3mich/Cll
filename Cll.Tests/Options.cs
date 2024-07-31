using System.IO.Ports;
using FluentValidation;

namespace Cll;

public class Options
{
    [Argument("v", "view")]
    public string? View { get; set; }

    [Argument("w", "write")]
    public string? FileName { get; set; }

    [Argument("s", "size")]
    [UsedBy(nameof(FileName))]
    public int Size { get; set; }

    [Argument("t", "width")]
    public int Width { get; set; }

    [Option(0)]
    [Mandatory]
    [UsedBy(nameof(Device))]
    public int BaudRate { get; set; }

    [Argument("p", "parity")]
    public Parity Parity { get; set; }

    [Option(1)]
    [Mandatory]
    public string? Device { get; set; }

    public class Validator : AbstractValidator<Options>
    {
        public Validator()
        {
            RuleFor(s => s.Device)
                .NotEmpty();

            When(s => s.Device != null, () =>
            {
                RuleFor(s => s.BaudRate).NotEmpty();
            });

            When(s => s.FileName != null, () =>
            {
                RuleFor(s => s.Size).GreaterThan(0);
            });
        }
    }
}
