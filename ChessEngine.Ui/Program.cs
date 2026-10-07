using ChessEngine.Core;
using ChessEngine.Ui;
using ChessEngine.Ui.Renderer;

var fen = args.Length > 0 ? args[0] : Fen.StartPosition;
var board = Fen.Parse(fen);

var layout = new BoardLayout(originX: 50, originY: 50, squareSize: 150);

IRenderer[] renderers = 
[
    new BoardRenderer(layout),
    new PieceRenderer(board, layout)
];

var app = new ChessApplication(renderers);
app.Run();
