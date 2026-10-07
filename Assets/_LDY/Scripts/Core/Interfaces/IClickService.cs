using System;

public interface IClickService
{
    event Action<int> OnClicked;
    void Click();
}
