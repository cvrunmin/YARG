using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using Unity.Barracuda;

namespace YARG.Gen
{
    /**
     * Integration of Note Action Model is cancelled
     **/
    public class NoteActionPlacer
    {
        private static NoteActionPlacer _inst;

        public static NoteActionPlacer Instance
        {
            get
            {
                if (_inst == null) _inst = new NoteActionPlacer();
                return _inst;
            }
        }

        //private Model model;

        //public IWorker Engine { get; private set; }

        //private NoteActionPlacer()
        //{
        //    model = ModelLoader.LoadFromStreamingAssets("action_placement.onnx");
        //    Engine = WorkerFactory.CreateComputeWorker(model);
        //}
    }
}
