using Meadow;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;
using StopSignDetection;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MeadowDesktopAIApp;

public class MeadowApp : App<Desktop>
{
    // Hardware
    const int displayWidth = 1280;
    const int displayHeight = 720;

    // Controllers
    DisplayController? displayController;
    InputController? inputController;

    MLContext? context;
    IDataView? mlData;
    string[]? modelLabels;
    TransformerChain<Microsoft.ML.Transforms.Onnx.OnnxTransformer>? model;
    PredictionEngine<StopSignInput, StopSignPrediction>? predictionEngine;

    // Images
    // Loaded from resources (could be from a camera)
    string[] imagesCollection = { "image1.bmp", "image2.bmp", "image3.bmp" };
    int currentImage = 2;
    bool imageDisplayed = false;

    // Display the next image in the list
    private void NextImage()
    {
        if (currentImage == imagesCollection.Length - 1)
        {
            currentImage = 0;
        }
        else
        {
            currentImage++;
        }
        displayController!.DrawImage(Image.LoadFromResource(imagesCollection[currentImage]));
        imageDisplayed = true;
    }

    // Display the previous image in the list
    private void PreviousImage()
    {
        if (currentImage == 0)
        {
            currentImage = imagesCollection.Length - 1;
        }
        else
        {
            currentImage--;
        }
        displayController!.DrawImage(Image.LoadFromResource(imagesCollection[currentImage]));
        imageDisplayed = true;
    }

    // Run Onnx model on currently displayed image and overlay bounding boxes on it
    private void RunModelOnCurrrentImage()
    {
        if (imageDisplayed)
        {
            Resolver.Log.Info("Running Onnx model on displayed image.");

            // Load image from resources
            var mlImage = LoadMLImage(Assembly.GetExecutingAssembly().GetName().Name+"."+imagesCollection[currentImage]);

            // Run model on image
            var prediction = predictionEngine!.Predict(new StopSignInput { image = mlImage });
            
            // Draw bounding boxes on image
            var boundingBoxes = prediction.BoundingBoxes.Chunk(prediction.BoundingBoxes.Count() / prediction.PredictedLabels.Count());
            var originalWidth = mlImage.Width;
            var originalHeight = mlImage.Height;
            for (int i = 0; i < boundingBoxes.Count(); i++)
            {
                var boundingBox = boundingBoxes.ElementAt(i);
                var left = boundingBox[0] * originalWidth;
                var top = boundingBox[1] * originalHeight;
                var right = boundingBox[2] * originalWidth;
                var bottom = boundingBox[3] * originalHeight;
                int x = (int)left;
                int y = (int)top;
                int width = (int)Math.Abs(right - left);
                int height = (int)Math.Abs(top - bottom);
                var label = modelLabels![prediction.PredictedLabels[i]];
                var score = prediction.Scores[i];

                displayController!.DrawMLBox(x, y, width, height, label, score, Color.Red);
                Resolver.Log.Info($"Recognized {label} with {score*100:0}% probability.");
            }
        }
    }

    // Load Image as a resource for the ML model
    private MLImage LoadMLImage(string resourceName)
    {
        Resolver.Log.Info($"Loading ML Image {resourceName}");

        var assembly = Assembly.GetExecutingAssembly();
        return MLImage.CreateFromStream(assembly.GetManifestResourceStream(resourceName));

    }

    // Load labels from text file
    private string[] LoadLabels(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        var labels = new List<string>();
        while (!reader.EndOfStream)
        {
            labels.Add(reader.ReadLine()!);
        }
        return labels.ToArray();
    }

    // Initialize app
    public override Task Initialize()
    {
        Resolver.Log.Info("Initializing...");

        // ---------------------
        // Initialize display
        Device.Display!.Resize(displayWidth, displayHeight, 1);
        displayController = new DisplayController(Device.Display!);

        // ---------------------
        // Initialize input controller
        inputController = new InputController();
        inputController.NextRequested += (s, e) => NextImage();
        inputController.PreviousRequested += (s, e) => PreviousImage();
        inputController.EnterRequested += (s, e) => RunModelOnCurrrentImage();

        // ---------------------
        // Initialize ML model resources
        context = new MLContext();
        mlData = context.Data.LoadFromEnumerable(new List<StopSignInput>());

        modelLabels = LoadLabels("MeadowDesktopAIApp.labels.txt");

        var pipeline = context.Transforms.ResizeImages(
            resizing: ImageResizingEstimator.ResizingKind.Fill, 
            outputColumnName: "image_tensor", 
            imageWidth: ImageSettings.imageWidth, 
            imageHeight: ImageSettings.imageHeight, 
            inputColumnName: nameof(StopSignInput.image))
                                .Append(context.Transforms.ExtractPixels(outputColumnName: "image_tensor"))
                                .Append(context.Transforms.ApplyOnnxModel(outputColumnNames: new string[] { "detected_boxes", "detected_scores", "detected_classes" },
                                                                          inputColumnNames: new string[] { "image_tensor" },
                                                                          modelFile: "./model.onnx"));

        model = pipeline.Fit(mlData);

        predictionEngine = context.Model.CreatePredictionEngine<StopSignInput, StopSignPrediction>(model);

        
        return base.Initialize();
    }

    public override Task Run()
    {
        // NOTE: this will not return until the display is closed
        ExecutePlatformDisplayRunner();

        return Task.CompletedTask;
    }

    private void ExecutePlatformDisplayRunner()
    {
        if (Device.Display is SilkDisplay sd)
        {
            sd.Run();
        }
        MeadowOS.TerminateRun();
        System.Environment.Exit(0);
    }
}