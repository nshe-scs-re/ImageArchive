from data import PhenoCamDataModule
from model import PhenoCamResNet
import pytorch_lightning as pl

dm = PhenoCamDataModule(
    site_name="delnortecounty2",
    train_dir="/Users/nathanmichelotti/Desktop/Senior Model/my_data/Train",
    train_labels="train_labels.csv",
    test_dir="/Users/nathanmichelotti/Desktop/Senior Model/my_data/Test",
    test_labels="test_labels.csv",  # Use the real test labels now
    batch_size=16
)

dm.prepare_data()  # No download/label arguments needed
dm.setup(stage="fit")

model = PhenoCamResNet(resnet="resnet18", n_classes=3)
trainer = pl.Trainer(max_epochs=10)
trainer.fit(model, dm)