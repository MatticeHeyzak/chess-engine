using ChessEngine.Core.BoardState;
using ChessEngine.Core.Main;
using ChessEngine.Ui;
using ChessEngine.Ui.Renderer;
using ChessEngine.Ui.Updates;

var fen = args.Length > 0 ? args[0] : Fen.StartPosition;
var board = Fen.Parse(fen);

const int boardMargin = 50;
const int squareSize = 150;
const int botThinkTimeMs = 1000;
var layout = new BoardLayout(originX: boardMargin, originY: boardMargin, squareSize: squareSize);
var interaction = new BoardInteraction(board, layout);

IRenderer[] renderers =
[
    new BoardRenderer(layout),
    new MoveHighlightRenderer(layout, interaction),
    new PieceRenderer(board, layout),
    new PromotionPickerRenderer(board, layout, interaction)
];

var bot = new BotController(board, Piece.Black, thinkTimeMs: botThinkTimeMs);

var app = new ChessApplication(renderers, [interaction, bot]);
app.Run();
