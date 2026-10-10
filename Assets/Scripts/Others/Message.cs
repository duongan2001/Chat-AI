using System;

[Serializable]
public class Message
{
    #region VARIABLES

    public MessageType messageType;
   
    public bool isMessageFromUser;
    public string textContent;
    public string photoFilePath;

    #endregion


    #region MESSAGE GET METHODS

    public string GetMessageTextContent()
    {
        return textContent ?? string.Empty;
    }

    public string GetMessagePhotoFilePath()
    {
        return photoFilePath ?? string.Empty;
    }

    #endregion


    #region MESSAGE SET METHODS

    public void SetMessageTextContent(string value)
    {
        textContent = value ?? string.Empty;
    }

    public void SetMessagePhotoFilePath(string value)
    {
        photoFilePath = value ?? string.Empty;
    }

    #endregion
}
