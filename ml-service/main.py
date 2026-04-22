from fastapi import FastAPI
import joblib

app = FastAPI()

@app.post("/predict")
def predict(data: dict):
    # dummy logic for now
    risk = "High" if data["dti"] > 0.4 else "Low"
    return {"risk": risk}