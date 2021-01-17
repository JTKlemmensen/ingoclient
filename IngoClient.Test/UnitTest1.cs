using IngoClient.Interfaces;
using IngoClient.Models;
using Moq;
using System;
using System.Net.Http;
using Xunit;

namespace IngoClient.Test
{
    public class UnitTest1
    {
        [Fact]
        public void LoginBodyBuilder()
        {
            var user = new User
            {
                Username = "myMail@gmail.com",
                Password = "myPassword",
                DeviceName = "INGO : samsung SM-A505FN : Android 10"
            };
            var client = new IngoClientClass(new Mock<IConsole>().Object, new Mock<IHttpWrapper>().Object);
            var expected = "client_id=f890879a-3f2f-11e9-9293-b3f7a52580cb&scope=USER%20INGO_LOYALTY&grant_type=password&username=myMail%40gmail.com&password=myPassword&deviceName=INGO%20%3A%20samsung%20SM-A505FN%20%3A%20Android%2010";

            //Act
            var body = client.LoginBodyBuilder(user);

            Assert.Equal(expected, body);
        }

        [Fact]
        public void Login()
        {
            var user = new User
            {
                Username = "myMail@gmail.com",
                Password = "myPassword",
                DeviceName = "INGO : samsung SM-A505FN : Android 10"
            };
            var http = new Mock<IHttpWrapper>();
            http.Setup(m => m.Post("https://id.circlekeurope.com/api/v3/oauth/authorize/password", It.IsAny<HttpContent>()))
                .Returns(new HttpResponse { StatusCode = 200, ResponseBody = "{\"mfaRequired\":true,\"mfaSessionId\":\"0f8c6adc-a6e0-41f7-a665-585d7a05d8ff\"}" });
            var client = new IngoClientClass(new Mock<IConsole>().Object, http.Object);

            //Act
            client.Login(user);


        }
    }
}