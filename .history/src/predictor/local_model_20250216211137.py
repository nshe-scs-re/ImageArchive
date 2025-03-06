class model(nn.Module):
    def __init__(self, cnn_model="resnet18", sequence_length=5, hidden_size=128, num_classes=2):
        super(model, self).__init__()