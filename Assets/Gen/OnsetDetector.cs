using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using Unity.Barracuda;
using NWaves;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using NWaves.Windows;
using NWaves.Filters.Fda;

namespace YARG.Gen
{
    /**
     * Integration of Note Action Model is cancelled
     **/
    public class OnsetDetector
    {
        private static OnsetDetector _inst;

        public static OnsetDetector Instance {
            get
            {
                if (_inst == null) _inst = new OnsetDetector();
                return _inst;
            }
        }

        //private Model model;

        //public IWorker Engine { get; private set; }

        //private OnsetDetector()
        //{
        //    model = ModelLoader.LoadFromStreamingAssets("onset_detector.onnx");
        //    Engine = WorkerFactory.CreateComputeWorker(model);
        //}

        public void Predict(AudioClip clip, int difficulty)
        {
            //var samplingRate = clip.frequency;
            //var hopSize = samplingRate / 100;
            //var melCount = 80;

            //var samples = new float[clip.samples * clip.channels];
            //clip.GetData(samples, 0);
            //if(clip.channels > 1)
            //{
            //    var old = samples;
            //    samples = new float[clip.samples];
            //    for (int i = 0; i < samples.Length; i++)
            //    {
            //        var sum = 0f;
            //        for (int j = 0; j < clip.channels; j++)
            //        {
            //            sum += old[i * clip.channels + j];
            //        }
            //        samples[i] = sum / clip.channels;
            //    }
            //}
            //var tensors = new List<Tensor>();
            //foreach (var fft in new int []{ 1024, 2048, 4096})
            //{
            //    var mfccExtractor = new FilterbankExtractor(
            //       new FilterbankOptions
            //       {
            //           SamplingRate = samplingRate,
            //           FrameSize = fft,
            //           FftSize = fft,
            //           HopSize = hopSize,
            //           Window = WindowType.Hann,
            //           FilterBank = FilterBanks.Triangular(fft, samplingRate, FilterBanks.MelBands(melCount, samplingRate, lowFreq: 22.5, highFreq: 16000.0)),
            //           NonLinearity = NonLinearityType.LogE
            //       // if power = 1.0
            //       // SpectrumType = SpectrumType.Magnitude
            //   });
            //    var mf = mfccExtractor.ParallelComputeFrom(samples);
            //    tensors.Add(new Tensor(new TensorShape(mf.Count, 80), mf.ToArray()).Reshape(new TensorShape(mf.Count, 80, 1)));
            //}
            //var op = new ReferenceComputeOps();
            //var concated = op.Concat(tensors.ToArray(), 3);
            //var frameCount = concated.shape.Axis(0);
            //var zeros = new Tensor(new TensorShape(7, 80, 3));
            //zeros.Fill(0);
            //concated = op.Concat(new Tensor[] { zeros, concated, zeros }, 0);
            //var idx = new Tensor(new TensorShape(frameCount));
            //for (int i = 0; i < frameCount; i++)
            //{
            //    for (int j = 0; j < 15; j++)
            //    {
            //        idx[i * 15 + j] = i + j;
            //    }
            //}
            //var gathered = op.Gather(new Tensor[] { concated, idx }, 0);
            //var dt = op.OneHot(new Tensor(new int[] { 1 }, new float[] { difficulty }), 5, 1, 0);
            //dt = dt.Reshape(new TensorShape(1, 5));
            //dt = op.Tile(dt, new int[] { frameCount });
            //var inputs = new Dictionary<string, Tensor>();
            //inputs.Add("frq", gathered);
            //inputs.Add("difficulty", dt);
            //Engine.Execute(inputs).PeekOutput();
        }
    }
}
