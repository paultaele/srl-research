using System.Collections.Generic;
using System.Windows.Ink;

namespace Srl.Tools
{
    public interface Matcher
    {
        void Train(string dirPath);

        void Run(StrokeCollection test);

        string Label();

        List<string> Labels();

        StrokeCollection Result();

        List<StrokeCollection> Results();

        double Score(StrokeCollection template);

        StrokeCollection TrainingData(StrokeCollection template);
    }
}
