# train_local.py
from data import PhenoCamDataModule
from model import PhenoCamResNet
import pytorch_lightning as pl

dm = PhenoCamDataModule(
    site_name="delnortecounty2",
    train_dir="downloaded_delnortecounty2",
    train_labels="train_labels.csv",
    test_dir="downloaded_delnortecounty2",  # Use same dir if no test split
    test_labels="train_labels.csv",         # Temporary placeholder
    batch_size=16
)
dm.prepare_data()
dm.setup(stage="fit")

model = PhenoCamResNet(resnet="resnet18", n_classes=3)
trainer = pl.Trainer(max_epochs=10)
trainer.fit(model, dm)
