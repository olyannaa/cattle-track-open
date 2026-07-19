using CAT.Controllers.DTO.AiAssistant;
using CAT.Services.Ai;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiWriteAssistantMessagesTests
{
    [Fact]
    public void ForPreview_WhenAnimalIsAmbiguous_ExplainsWhatUserMustDo()
    {
        var message = AiWriteAssistantMessages.ForPreview(new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateInsemination,
            Items =
            {
                new AiWriteDraftItem
                {
                    Tag = "25",
                    Status = AiWriteItemStatus.Ambiguous,
                    Message = "Найдено несколько животных с биркой 25. Нужно уточнение."
                }
            }
        });

        Assert.Equal(
            "Пока ничего не сохранено. Нашлось несколько животных с биркой 25. Выберите нужное животное по карточке или уточните дату рождения либо дополнительный идентификатор.",
            message);
    }

    [Fact]
    public void ForCommit_WhenSingleItemWasSaved_ConfirmsTheBusinessResult()
    {
        var report = new AiWriteCommitReport(
            "v1",
            AiAssistantToolNames.CreateInsemination,
            Guid.NewGuid(),
            1,
            1,
            0,
            0,
            new[] { new AiWriteCommitItemReport(0, "item-1", "25", AiWriteItemStatus.Committed, "Сохранено.") },
            string.Empty,
            string.Empty);

        Assert.Equal(
            "Информация внесена: запись об осеменении для животного с биркой 25.",
            AiWriteAssistantMessages.ForCommit(report));
    }
}
