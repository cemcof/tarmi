namespace Betrian.Imaging.Algorithms.Focus;

public class WPeakFinder
{
    public static (int LowerBoundIndex, int UpperBoundIndex)? FindPeakBounds(IList<double> series, int stableComparisonsCount = 2)
    {
        var lower = series.Count;
        var upper = 0;
        // Look for the left slope
        for (int i = 0; i < series.Count - stableComparisonsCount; i++)
        {
            bool foundBound = true;
            for (int j = 0; j < stableComparisonsCount; j++)
            {
                if (series[i + j] >= series[i + j + 1])
                {
                    foundBound = false;
                    break;
                }
            }
            if (foundBound)
            {
                lower = i;
                break;
            }
        }
        // Look for the right slope
        for (int i = series.Count - 1; i >= stableComparisonsCount; i--)
        {
            bool foundBound = true;
            for (int offset = 0; offset < stableComparisonsCount; offset++)
            {
                if (series[i - offset - 1] <= series[i - offset])
                {
                    foundBound = false;
                    break;
                }
            }
            if (foundBound)
            {
                upper = i;
                break;
            }
        }
        return lower >= upper ? null : (lower, upper);
    }
}
