from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

class PredictionInput(BaseModel):
    dti: float
@app.post("/predict")
def predict(data: PredictionInput):
    risk = "High" if data.dti > 0.4 else "Low"
    return {"risk": risk}