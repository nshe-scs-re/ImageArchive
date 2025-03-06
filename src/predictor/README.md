# Image Archive Snow Detection

This project combines a Blazor frontend with a Python-based machine learning backend for snow detection in images.

## Prerequisites

1. .NET 7.0 or later
2. Python 3.8 or later
3. pip (Python package manager)

## Setup Instructions

1. Clone the repository
2. Install Python dependencies:
   ```bash
   cd src/predictor
   pip install -r requirements.txt
   ```

3. Run the Blazor application:
   ```bash
   cd src/frontend
   dotnet run
   ```

## Project Structure

- `src/frontend/` - Blazor web application
- `src/predictor/` - Python ML model and prediction scripts
  - `model.py` - ML model definition
  - `GUI_to_Test.py` - Standalone GUI testing tool
  - `requirements.txt` - Python dependencies

## Model Files

Make sure you have the following model files in place:
- `predictor/Local_data_for_model/best_model.pth` - Trained model weights
- `predictor/Local_data_for_model/classify_image.py` - Image classification script

## Notes

- The application requires both the .NET runtime and Python to be installed and accessible from the command line
- The ML model uses PyTorch and runs on CPU by default
- For GPU acceleration, ensure you have CUDA installed and the appropriate PyTorch version
