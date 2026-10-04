using ChessEngine.Ui;
using ChessEngine.Ui.Renderer;

IEnumerable<IRenderer> renderers = [new BoardRenderer()];
var app = new ChessApplication(renderers);
app.Run();
