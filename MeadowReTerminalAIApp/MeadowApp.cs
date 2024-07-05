using System.Threading.Tasks;
using Meadow;
using Meadow.Foundation.Displays;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;
using StopSignDetection;
using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Meadow.Peripherals.Displays;

namespace reTerminalApp;

public class MeadowApp : App<RaspberryPi>
{
    // Hardware
    GtkDisplay _display;
    const int displayWidth = 1280;
    const int displayHeight = 720;

    // Controllers
    DisplayController displayController;
    InputController inputController;

    // Onnx Model resources
    MLContext context;
    IDataView mlData;
    string[] modelLabels;
    TransformerChain<Microsoft.ML.Transforms.Onnx.OnnxTransformer> model;
    PredictionEngine<StopSignInput, StopSignPrediction> predictionEngine;

    // Images
    // Loaded from resources (could be from a camera)
    string[] imagesCollection = { "image1.bmp", "image2.bmp", "image3.bmp" };
    int currentImage = 2;
    bool imageDisplayed = false;

    // Display the next image in the list
    private void NextImage()
    {
        Resolver.Log.Info("Displaying previous image.");

        if (currentImage == imagesCollection.Length - 1)
        {
            currentImage = 0;
        }
        else
        {
            currentImage++;
        }
        displayController.DrawImage(Meadow.Foundation.Graphics.Image.LoadFromResource(imagesCollection[currentImage]));
        imageDisplayed = true;
    }

    // Display the previous image in the list
    private void PreviousImage()
    {
        Resolver.Log.Info("Displaying previous image.");
        if (currentImage == 0)
        {
            currentImage = imagesCollection.Length - 1;
        }
        else
        {
            currentImage--;
        }
        displayController.DrawImage(Meadow.Foundation.Graphics.Image.LoadFromResource(imagesCollection[currentImage]));
        imageDisplayed = true;
    }

    // Run Onnx model on currently displayed image
    private void RunModelOnCurrrentImage()
    {
        if (imageDisplayed)
        {
            Resolver.Log.Info("Running Onnx model on displayed image.");
            var mlImage = LoadMLImage("reTerminalApp.test." + imagesCollection[currentImage]);
            var prediction = predictionEngine.Predict(new StopSignInput { image = mlImage });
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
                var label = modelLabels[prediction.PredictedLabels[i]];
                var score = prediction.Scores[i];

                displayController.DrawMLBox(x, y, width, height, label, score, Color.Red);
                Resolver.Log.Info($"Recognized {label} with {score * 100:0}% probability.");
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
        using var reader = new StreamReader(stream);
        var labels = new List<string>();
        while (!reader.EndOfStream)
        {
            labels.Add(reader.ReadLine());
        }
        return labels.ToArray();
    }

    public static void ExtractResourceToFile(string resourceName, string filename)
    {
        Resolver.Log.Info($"Extracting resource {resourceName} to {filename}");

        // Get the current assembly
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Get the stream for the embedded resource
        using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
        {
            if (resourceStream == null)
            {
                throw new FileNotFoundException("The specified embedded resource cannot be found.", resourceName);
            }

            // Create the output file and write the resource to it
            using (Stream fileStream = File.Create(filename))
            {
                resourceStream.CopyTo(fileStream);
            }
        }
    }

    // Initialize app
    public override Task Initialize()
    {
        Resolver.Log.Info("Initialize...");

        // Initialize display
        _display = new GtkDisplay(displayWidth, displayHeight, ColorMode.Format16bppRgb565);
        displayController = new DisplayController(_display);

        // Initialize input controller
        inputController = new InputController();
        inputController.NextRequested += (s, e) => NextImage();
        inputController.PreviousRequested += (s, e) => PreviousImage();
        inputController.EnterRequested += (s, e) => RunModelOnCurrrentImage();

        // Initialize ML model resources
        context = new MLContext();
        mlData = context.Data.LoadFromEnumerable(new List<StopSignInput>());

        modelLabels = LoadLabels(Assembly.GetExecutingAssembly().GetName().Name+".labels.txt");

        ExtractResourceToFile(Assembly.GetExecutingAssembly().GetName().Name + ".model.onnx", "model2.onnx");

        var pipeline = context.Transforms.ResizeImages(
            resizing: ImageResizingEstimator.ResizingKind.Fill,
            outputColumnName: "image_tensor",
            imageWidth: ImageSettings.imageWidth,
            imageHeight: ImageSettings.imageHeight,
            inputColumnName: nameof(StopSignInput.image)).Append(context.Transforms.ExtractPixels(outputColumnName: "image_tensor")).Append(context.Transforms.ApplyOnnxModel(outputColumnNames: new string[] { "detected_boxes", "detected_scores", "detected_classes" }, inputColumnNames: new string[] { "image_tensor" }, modelFile: "./model2.onnx"));

        model = pipeline.Fit(mlData);

        predictionEngine = context.Model.CreatePredictionEngine<StopSignInput, StopSignPrediction>(model);


        return Task.CompletedTask;
    }

    // Run app
    public override async Task Run()
    {
        Resolver.Log.Info("Hello, reTerminal!");

        // Start listening to input from user
        inputController.StartListeningForKeyboardEvents();

        await Task.Run(_display.Run);

    }

    public static async Task Main(string[] args)
    {
        await MeadowOS.Start(args);
    }
}