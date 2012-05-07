
namespace Unplugged.Segy
{
	public interface IReadingProgress
	{
		void ReportProgress(int progressPercentage);
		bool CancellationPending { get; }
	}
}

