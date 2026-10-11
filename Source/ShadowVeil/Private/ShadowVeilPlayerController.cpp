#include "ShadowVeilPlayerController.h"
#include "ShadowVeilPawn.h"
#include "Engine/World.h"
#include "GameFramework/PlayerInput.h"

void AShadowVeilPlayerController::BeginPlay()
{
	Super::BeginPlay();
	bShowMouseCursor = true;
	bEnableClickEvents = true;
	bEnableTouchEvents = true;
	FInputModeGameOnly InputMode;
	SetInputMode(InputMode);
}

void AShadowVeilPlayerController::SetupInputComponent()
{
	Super::SetupInputComponent();
	InputComponent->BindKey(EKeys::LeftMouseButton, IE_Pressed, this, &AShadowVeilPlayerController::BeginDrag);
	InputComponent->BindKey(EKeys::LeftMouseButton, IE_Released, this, &AShadowVeilPlayerController::EndDrag);
	InputComponent->BindTouch(IE_Pressed, this, &AShadowVeilPlayerController::TouchBegin);
	InputComponent->BindTouch(IE_Repeat, this, &AShadowVeilPlayerController::TouchMove);
	InputComponent->BindTouch(IE_Released, this, &AShadowVeilPlayerController::TouchEnd);
}

void AShadowVeilPlayerController::BeginDrag()
{
	bDragging = true;
	UpdateDrag();
}

void AShadowVeilPlayerController::UpdateDrag()
{
	float X = 0.0f, Y = 0.0f;
	if (bDragging && GetMousePosition(X, Y)) SetDestinationFromScreenPosition(FVector2D(X, Y));
}

void AShadowVeilPlayerController::EndDrag()
{
	UpdateDrag();
	bDragging = false;
}

void AShadowVeilPlayerController::TouchBegin(ETouchIndex::Type, FVector Location)
{
	bDragging = true;
	SetDestinationFromScreenPosition(FVector2D(Location.X, Location.Y));
}

void AShadowVeilPlayerController::TouchMove(ETouchIndex::Type, FVector Location)
{
	if (bDragging) SetDestinationFromScreenPosition(FVector2D(Location.X, Location.Y));
}

void AShadowVeilPlayerController::TouchEnd(ETouchIndex::Type, FVector Location)
{
	if (bDragging) SetDestinationFromScreenPosition(FVector2D(Location.X, Location.Y));
	bDragging = false;
}

void AShadowVeilPlayerController::SetDestinationFromScreenPosition(const FVector2D& ScreenPosition)
{
	FVector RayOrigin, RayDirection;
	if (!DeprojectScreenPositionToWorld(ScreenPosition.X, ScreenPosition.Y, RayOrigin, RayDirection)) return;
	if (FMath::IsNearlyZero(RayDirection.Z)) return;
	const float Distance = -RayOrigin.Z / RayDirection.Z;
	if (Distance < 0.0f) return;
	const FVector GroundPoint = RayOrigin + RayDirection * Distance;
	if (AShadowVeilPawn* Hero = Cast<AShadowVeilPawn>(GetPawn())) Hero->SetMoveDestination(GroundPoint);
}
