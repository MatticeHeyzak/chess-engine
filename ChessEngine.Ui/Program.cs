using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Ui;
using ChessEngine.Ui.Renderer;
using ChessEngine.Ui.Updates;

var fen = args.Length > 0 ? args[0] : Fen.StartPosition;
var board = Fen.Parse(fen);

var layout = new BoardLayout(originX: 50, originY: 50, squareSize: 150);
var interaction = new BoardInteraction(board, layout);

IRenderer[] renderers = 
[
    new BoardRenderer(layout),
    new MoveHighlightRenderer(layout, interaction),
    new PieceRenderer(board, layout),
    new PromotionPickerRenderer(board, layout, interaction)
];

var bot = new BotController(board, Piece.Black, thinkTimeMs: 1000);

var app = new ChessApplication(renderers, [interaction, bot]);
app.Run();
