#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "ShadowVeilPlayerController.generated.h"

UCLASS()
class SHADOWVEIL_API AShadowVeilPlayerController : public APlayerController
{
	GENERATED_BODY()

protected:
	virtual void BeginPlay() override;
	virtual void SetupInputComponent() override;

private:
	void BeginDrag();
	void UpdateDrag();
	void EndDrag();
	void TouchBegin(ETouchIndex::Type FingerIndex, FVector Location);
	void TouchMove(ETouchIndex::Type FingerIndex, FVector Location);
	void TouchEnd(ETouchIndex::Type FingerIndex, FVector Location);
	void SetDestinationFromScreenPosition(const FVector2D& ScreenPosition);
	bool bDragging = false;
};
